using System.Data;
using AppointMan.Application.Common.Abstractions;
using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Errors;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Commands.CreateAppointment
{
    public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Result<Guid>>
    {
        private const string BlacklistMessage = "Randevunuz oluşturulamıyor, lütfen işletme ile iletişime geçiniz.";

        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<BlacklistEntry> _blacklistRepository;
        private readonly IRepository<IdempotencyRecord> _idempotencyRepository;
        private readonly IAppointmentBookingGuard _bookingGuard;
        private readonly IDateTimeService _dateTimeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppointManMetrics _metrics;
        private readonly ICurrentUserService _currentUser;

        public CreateAppointmentCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            IRepository<Employee> employeeRepository,
            IRepository<User> userRepository,
            IRepository<BlacklistEntry> blacklistRepository,
            IRepository<IdempotencyRecord> idempotencyRepository,
            IAppointmentBookingGuard bookingGuard,
            IDateTimeService dateTimeService,
            IUnitOfWork unitOfWork,
            AppointManMetrics metrics,
            ICurrentUserService currentUser)
        {
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _employeeRepository = employeeRepository;
            _userRepository = userRepository;
            _blacklistRepository = blacklistRepository;
            _idempotencyRepository = idempotencyRepository;
            _bookingGuard = bookingGuard;
            _dateTimeService = dateTimeService;
            _unitOfWork = unitOfWork;
            _metrics = metrics;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                // Govdeye guevenilmez; istegi yapan kisi ambient kimlikten belirlenir.
                var requesterId = _currentUser.UserId.Value;
                request.RequesterId = requesterId;
                var now = _dateTimeService.UtcNow;

                // Butun kontroller ve yazmalar ayni SERIALIZABLE transaction icinde.
                var result = await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    // 0. Idempotency: anahtar istegi yapan kisiyle birlikte scope'lanir.
                    if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
                    {
                        var existingRecords = await _idempotencyRepository.FindAsync(
                            r => r.Key == request.IdempotencyKey && r.UserId == request.RequesterId,
                            token);

                        var existingRecord = existingRecords.FirstOrDefault();
                        if (existingRecord is not null)
                            return Result<Guid>.Success(existingRecord.AppointmentId);
                    }

                    // 1. Requester kontrolu
                    if (await _userRepository.GetByIdAsync(request.RequesterId, token) is null)
                        return Result<Guid>.Failure(Error.NotFound("Kullanici", request.RequesterId.ToString()));

                    // 2. Isletme kontrolu
                    var business = await _businessRepository.GetByIdWithIncludesAsync(
                        request.BusinessId,
                        token,
                        b => b.WorkingHours);

                    if (business is null)
                        return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                    if (!business.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("Isletme aktif degil."));

                    // 3. Hizmet kontrolu
                    var service = await _serviceRepository.GetByIdAsync(request.ServiceId, token);
                    if (service is null)
                        return Result<Guid>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                    if (!service.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("Hizmet aktif degil."));

                    if (service.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.BusinessRule("Hizmet bu isletmeye ait degil."));

                    // 4. Calisan kontrolu
                    var employee = await _employeeRepository.GetByIdWithIncludesAsync(
                        request.EmployeeId,
                        token,
                        e => e.EmployeeServices,
                        e => e.WorkingHours,
                        e => e.TimeOffs);

                    if (employee is null)
                        return Result<Guid>.Failure(Error.NotFound("Calisan", request.EmployeeId.ToString()));

                    if (!employee.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("Calisan aktif degil."));

                    if (!employee.IsAvailableForBooking)
                        return Result<Guid>.Failure(Error.BusinessRule("Calisan su anda randevu kabul etmiyor."));

                    if (employee.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.BusinessRule("Calisan bu isletmeye ait degil."));

                    // 5. Calisan bu hizmeti verebiliyor mu?
                    if (!employee.CanProvideService(request.ServiceId))
                        return Result<Guid>.Failure(Error.BusinessRule("Calisan bu hizmeti veremiyor."));

                    // 6. Yetki kontrolu - kimlik JWT uzerinden cozulur.
                    var authorizationResult = CheckAuthorization(request, business);
                    if (authorizationResult.IsFailure)
                        return Result<Guid>.Failure(authorizationResult.Error!);

                    // 7. Calisma saati kontrolu. Istek UTC anidir; mesai saatleri isletme
                    //    yerel duvar saatinde tutulur. Tum aralik yerel zamanda kontrol edilir.
                    var start = request.AppointmentDate;
                    var durationMinutes = service.DurationInMinutes;
                    var end = start.AddMinutes(durationMinutes);
                    var localStart = _dateTimeService.ToBusinessLocalTime(start);
                    var localEnd = localStart.AddMinutes(durationMinutes);

                    if (!business.IsOpenDuring(localStart, localEnd))
                        return Result<Guid>.Failure(Error.BusinessRule("Isletme bu saatte calismiyor."));

                    if (!employee.IsWorkingDuring(localStart, localEnd))
                        return Result<Guid>.Failure(Error.BusinessRule("Calisan bu saatte calismiyor."));

                    if (employee.IsOnApprovedTimeOff(localStart, localEnd))
                        return Result<Guid>.Failure(Error.BusinessRule("Calisan bu tarihte izinde."));

                    // 8. Kara liste kontrolu
                    if (await IsBlacklistedAsync(request.UserId, request.BusinessId, token))
                        return Result<Guid>.Failure(Error.BusinessRule(BlacklistMessage));

                    // 9. 3. aktif randevu kontrolu (kayit yazmasi cakisma sonrasina birakilir)
                    var activeAppointmentCount = await CountActiveAppointmentsAsync(
                        request.UserId, request.BusinessId, now, token);
                    var activeLimitReached = activeAppointmentCount >= 2;

                    // 10. Cakisma kontrolu: SERIALIZABLE + aralik kilidi ile tek transaction.
                    var hasConflict = await _bookingGuard.HasConflictAsync(
                        request.EmployeeId,
                        start,
                        end,
                        durationMinutes,
                        excludeAppointmentId: null,
                        cancellationToken: token);

                    if (hasConflict)
                        return Result<Guid>.Failure(ConcurrencyErrors.BookingConflict());

                    // 11. Cakisma sonrasi kara liste tekrar kontrolu.
                    if (await IsBlacklistedAsync(request.UserId, request.BusinessId, token))
                        return Result<Guid>.Failure(Error.BusinessRule(BlacklistMessage));

                    // 12. Limit asildiysa kara listeye yazma ve kullanici kilidi ayni transaction'da.
                    if (activeLimitReached)
                    {
                        var blacklistEntry = new BlacklistEntry(
                            request.UserId,
                            request.BusinessId,
                            activeAppointmentCount + 1,
                            "Pes pese 3 aktif randevu");

                        await _blacklistRepository.AddAsync(blacklistEntry, token);
                        await CheckAndLockUserIfNeededAsync(request.UserId, token);

                        return Result<Guid>.Failure(Error.BusinessRule(BlacklistMessage));
                    }

                    // 13. Randevu + idempotency kaydi
                    var appointment = new Appointment(
                        request.UserId,
                        request.BusinessId,
                        request.ServiceId,
                        request.EmployeeId,
                        start,
                        request.Notes);

                    // Bitis saati her zaman yazilir; cakisma penceresi DB ile ayni olur.
                    appointment.SetEndTime(end);

                    // Domain tarafindaki gecmis tarih kontrolu ayni referans zamani kullanir.
                    appointment.SetAppointmentDate(start, now);

                    await _appointmentRepository.AddAsync(appointment, token);

                    if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
                    {
                        var record = new IdempotencyRecord(
                            request.IdempotencyKey, request.RequesterId, appointment.Id, now);

                        await _idempotencyRepository.AddAsync(record, token);
                    }

                    return Result<Guid>.Success(appointment.Id);
                }, IsolationLevel.Serializable, cancellationToken);

                // Olcum yalnizca gercekten yeni randevu olusursa artar.
                if (result.IsSuccess)
                    _metrics.RecordAppointmentCreated();

                return result;
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(Error.Internal("Randevu olusturulurken bir hata olustu."));
            }
        }

        // ===== YETKI KONTROLU =====
        private Result CheckAuthorization(
            CreateAppointmentCommand request,
            Business business)
        {
            // KURAL 1: Caller kendisi icin randevu olusturuyor veya yonetici.
            if (CallerAuthorization.CanActAsUser(_currentUser, request.UserId))
                return Result.Success();

            // KURAL 2: Isletme sahibi calisanina randevu olusturuyor.
            if (CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                return Result.Success();

            // KURAL 3: Baska birine randevu olusturma yetkisi yok.
            return Result.Failure(
                Error.Forbidden("Baska bir kullanici adina randevu olusturma yetkiniz yok."));
        }

        // ===== KARA LISTE KONTROLU =====
        private async Task<bool> IsBlacklistedAsync(
            Guid userId,
            Guid businessId,
            CancellationToken cancellationToken)
        {
            return await _blacklistRepository.ExistsAsync(
                b => b.UserId == userId &&
                     b.BusinessId == businessId &&
                     !b.IsDeleted,
                cancellationToken);
        }

        // ===== 3. AKTIF RANDEVU KONTROLU =====
        private async Task<int> CountActiveAppointmentsAsync(
            Guid userId,
            Guid businessId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            // Aktif = tarihi gelmemis, iptal edilmemis, tamamlanmamis
            var appointments = await _appointmentRepository.FindAsync(
                a => a.UserId == userId &&
                     a.BusinessId == businessId &&
                     a.Status != EnumAppointmentStatus.Cancelled &&
                     a.Status != EnumAppointmentStatus.Completed &&
                     a.AppointmentDate > now &&
                     !a.IsDeleted,
                cancellationToken);

            return appointments.Count;
        }

        // ===== 3 FARKLI ISLETMEDE KARA LISTEDE MI? =====
        private async Task CheckAndLockUserIfNeededAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            // Bu kullanicinin kara listede oldugu farkli isletme sayisi
            var blacklistEntries = await _blacklistRepository.FindAsync(
                b => b.UserId == userId && !b.IsDeleted,
                cancellationToken);

            var distinctBusinessCount = blacklistEntries
                .Select(b => b.BusinessId)
                .Distinct()
                .Count();

            if (distinctBusinessCount >= 3)
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user is not null && user.AccountStatus != EnumAccountStatus.Locked)
                {
                    user.LockAccount("3 farkli isletmede kara listeye girildi", "System");
                }
            }
        }
    }
}
