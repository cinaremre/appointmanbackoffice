using System.Data;
using Rezervise.Application.Common.Abstractions;
using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Commands.CreateAppointment
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
        private readonly RezerviseMetrics _metrics;
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
            RezerviseMetrics metrics,
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

                // Gövdeye guevenilmez; isteği yapan kişi ambient kimlikten belirlenir.
                var requesterId = _currentUser.UserId.Value;
                request.RequesterId = requesterId;
                var now = _dateTimeService.UtcNow;

                // Bütün kontroller ve yazmalar aynı SERIALIZABLE transaction içinde.
                var result = await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    // 0. Idempotency: anahtar isteği yapan kişiyle birlikte scope'lanır.
                    if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
                    {
                        var existingRecords = await _idempotencyRepository.FindAsync(
                            r => r.Key == request.IdempotencyKey && r.UserId == request.RequesterId,
                            token);

                        var existingRecord = existingRecords.FirstOrDefault();
                        if (existingRecord is not null)
                            return Result<Guid>.Success(existingRecord.AppointmentId);
                    }

                    // 1. Requester kontrolü
                    if (await _userRepository.GetByIdAsync(request.RequesterId, token) is null)
                        return Result<Guid>.Failure(Error.NotFound("Kullanıcı", request.RequesterId.ToString()));

                    // 2. İşletme kontrolü
                    var business = await _businessRepository.GetByIdWithIncludesAsync(
                        request.BusinessId,
                        token,
                        b => b.WorkingHours);

                    if (business is null)
                        return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                    if (!business.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("İşletme aktif değil."));

                    // Backoffice onayı gelmeyen işletme randevu kabul edemez:
                    // kayıt sonrası işletme, insan incelemesi tamamlanana kadar çalışmaz.
                    if (!business.IsApproved)
                        return Result<Guid>.Failure(
                            Error.BusinessRule("İşletme henüz onaylanmadı; randevu kabul edemez."));

                    // 3. Hizmet kontrolü
                    var service = await _serviceRepository.GetByIdAsync(request.ServiceId, token);
                    if (service is null)
                        return Result<Guid>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                    if (!service.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("Hizmet aktif değil."));

                    if (service.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.BusinessRule("Hizmet bu işletmeye ait değil."));

                    // 4. Çalışan kontrolü
                    var employee = await _employeeRepository.GetByIdWithIncludesAsync(
                        request.EmployeeId,
                        token,
                        e => e.EmployeeServices,
                        e => e.WorkingHours,
                        e => e.TimeOffs);

                    if (employee is null)
                        return Result<Guid>.Failure(Error.NotFound("Çalışan", request.EmployeeId.ToString()));

                    if (!employee.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan aktif değil."));

                    if (!employee.IsAvailableForBooking)
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan su anda randevu kabul etmiyor."));

                    if (employee.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan bu işletmeye ait değil."));

                    // 5. Çalışan bu hizmeti verebiliyor mü?
                    if (!employee.CanProvideService(request.ServiceId))
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan bu hizmeti veremiyor."));

                    // 6. Yetki kontrolü - kimlik JWT üzerinden çözülür.
                    var authorizationResult = CheckAuthorization(request, business);
                    if (authorizationResult.IsFailure)
                        return Result<Guid>.Failure(authorizationResult.Error!);

                    // 7. Çalışma saati kontrolü. İstek UTC anıdır; mesai saatleri işletme
                    //    yerel duvar saatinde tutulur. Tüm aralık yerel zamanda kontrol edilir.
                    var start = request.AppointmentDate;
                    var durationMinutes = service.DurationInMinutes;
                    var end = start.AddMinutes(durationMinutes);
                    var localStart = _dateTimeService.ToBusinessLocalTime(start);
                    var localEnd = localStart.AddMinutes(durationMinutes);

                    if (!business.IsOpenDuring(localStart, localEnd))
                        return Result<Guid>.Failure(Error.BusinessRule("İşletme bu saatte çalışmıyor."));

                    if (!employee.IsWorkingDuring(localStart, localEnd))
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan bu saatte çalışmıyor."));

                    if (employee.IsOnApprovedTimeOff(localStart, localEnd))
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan bu tarihte izinde."));

                    // 8. Kara liste kontrolü
                    if (await IsBlacklistedAsync(request.UserId, request.BusinessId, token))
                        return Result<Guid>.Failure(Error.BusinessRule(BlacklistMessage));

                    // 9. 3. aktif randevu kontrolü (kayıt yazması çakışma sonrasına bırakılır)
                    var activeAppointmentCount = await CountActiveAppointmentsAsync(
                        request.UserId, request.BusinessId, now, token);
                    var activeLimitReached = activeAppointmentCount >= 2;

                    // 10. Çakışma kontrolü: SERIALIZABLE + aralık kilidi ile tek transaction.
                    var hasConflict = await _bookingGuard.HasConflictAsync(
                        request.EmployeeId,
                        start,
                        end,
                        durationMinutes,
                        excludeAppointmentId: null,
                        cancellationToken: token);

                    if (hasConflict)
                        return Result<Guid>.Failure(ConcurrencyErrors.BookingConflict());

                    // 11. Çakışma sonrası kara liste tekrar kontrolü.
                    if (await IsBlacklistedAsync(request.UserId, request.BusinessId, token))
                        return Result<Guid>.Failure(Error.BusinessRule(BlacklistMessage));

                    // 12. Limit asıldıysa kara listeye yazma ve kullanıcı kilidi aynı transaction'da.
                    if (activeLimitReached)
                    {
                        var blacklistEntry = new BlacklistEntry(
                            request.UserId,
                            request.BusinessId,
                            activeAppointmentCount + 1,
                            "Peş peşe 3 aktif randevu");

                        await _blacklistRepository.AddAsync(blacklistEntry, token);
                        await CheckAndLockUserIfNeededAsync(request.UserId, token);

                        return Result<Guid>.Failure(Error.BusinessRule(BlacklistMessage));
                    }

                    // 13. Randevu + idempotency kaydı
                    var appointment = new Appointment(
                        request.UserId,
                        request.BusinessId,
                        request.ServiceId,
                        request.EmployeeId,
                        start,
                        request.Notes);

                    // Bitiş saati her zaman yazılır; çakışma penceresi DB ile aynı olur.
                    appointment.SetEndTime(end);

                    // Domain tarafındaki geçmiş tarih kontrolü aynı referans zamanı kullanır.
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

                // Ölçüm yalnızca gerçekten yeni randevu oluşursa artar.
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
                return Result<Guid>.Failure(Error.Internal("Randevu oluşturulurken bir hata oluştu."));
            }
        }

        // ===== YETKI KONTROLU =====
        private Result CheckAuthorization(
            CreateAppointmentCommand request,
            Business business)
        {
            // KURAL 1: Caller kendisi için randevu oluşturuyor veya yönetici.
            if (CallerAuthorization.CanActAsUser(_currentUser, request.UserId))
                return Result.Success();

            // KURAL 2: İşletme sahibi çalışanına randevu oluşturuyor.
            if (CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                return Result.Success();

            // KURAL 3: Başka birine randevu oluşturma yetkisi yok.
            return Result.Failure(
                Error.Forbidden("Başka bir kullanıcı adına randevu oluşturma yetkiniz yok."));
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
            // Aktif = tarihi gelmemiş, iptal edilmemiş, tamamlanmamış
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
            // Bu kullanıcının kara listede öldüğü farklı işletme sayısı
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
                    user.LockAccount("3 farklı işletmede kara listeye girildi", "System");
                }
            }
        }
    }
}
