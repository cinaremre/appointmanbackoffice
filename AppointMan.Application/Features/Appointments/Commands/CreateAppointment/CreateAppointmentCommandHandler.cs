using System.Data;
using AppointMan.Application.Common.Abstractions;
using AppointMan.Application.Common.Errors;
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
            IUnitOfWork unitOfWork)
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
        }

        public async Task<Result<Guid>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Idempotency: ayni anahtarla daha once randevu uretilmis mi?
                if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
                {
                    var existingRecords = await _idempotencyRepository.FindAsync(
                        r => r.Key == request.IdempotencyKey,
                        cancellationToken);

                    var existingRecord = existingRecords.FirstOrDefault();
                    if (existingRecord is not null)
                        return Result<Guid>.Success(existingRecord.AppointmentId);
                }

                // 1. Requester kontrolu
                var requester = await _userRepository.GetByIdAsync(request.RequesterId, cancellationToken);
                if (requester is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.RequesterId.ToString()));

                // 2. Isletme kontrolu
                var business = await _businessRepository.GetByIdWithIncludesAsync(
                    request.BusinessId,
                    cancellationToken,
                    b => b.WorkingHours);

                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                if (!business.IsActive)
                    return Result<Guid>.Failure(Error.BusinessRule("Isletme aktif degil."));

                // 3. Hizmet kontrolu
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null)
                    return Result<Guid>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                if (!service.IsActive)
                    return Result<Guid>.Failure(Error.BusinessRule("Hizmet aktif degil."));

                if (service.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Hizmet bu isletmeye ait degil."));

                // 4. Calisan kontrolu
                var employee = await _employeeRepository.GetByIdWithIncludesAsync(
                    request.EmployeeId,
                    cancellationToken,
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

                // 6. Yetki kontrolu
                var authorizationResult = CheckAuthorization(request, business);
                if (authorizationResult.IsFailure)
                    return Result<Guid>.Failure(authorizationResult.Error!);

                // 7. Calisma saati kontrolu
                if (!business.IsOpenOn(request.AppointmentDate))
                    return Result<Guid>.Failure(Error.BusinessRule("Isletme bu saatte calismiyor."));

                if (!employee.IsWorkingOn(request.AppointmentDate))
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu saatte calismiyor."));

                // 8. Calisan izinde mi?
                if (employee.IsOnTimeOff(request.AppointmentDate))
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu tarihte izinde."));

                // 9. Kara liste kontrolu
                var isBlacklisted = await _blacklistRepository.ExistsAsync(
                    b => b.UserId == request.UserId &&
                         b.BusinessId == request.BusinessId &&
                         !b.IsDeleted,
                    cancellationToken);

                if (isBlacklisted)
                    return Result<Guid>.Failure(
                        Error.BusinessRule("Randevunuz oluşturulamıyor, lütfen işletme ile iletişime geçiniz."));

                // 10. 3. aktif randevu kontrolu
                var activeAppointmentCount = await CountActiveAppointmentsAsync(
                    request.UserId, request.BusinessId, cancellationToken);

                if (activeAppointmentCount >= 2)
                {
                    // 3. randevu engelleniyor + kara listeye ekleniyor
                    var blacklistEntry = new BlacklistEntry(
                        request.UserId,
                        request.BusinessId,
                        activeAppointmentCount + 1,
                        "Pes pese 3 aktif randevu");

                    await _blacklistRepository.AddAsync(blacklistEntry, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Kullanici 3 farkli isletmede kara listede mi?
                    await CheckAndLockUserIfNeededAsync(request.UserId, cancellationToken);

                    return Result<Guid>.Failure(
                        Error.BusinessRule("Randevunuz oluşturulamıyor, lütfen işletme ile iletişime geçiniz."));
                }

                // 11. Cakisma kontrolu + kayit: aralik kilidi altinda tek transaction.
                //     SERIALIZABLE + UPDLOCK/HOLDLOCK sayesinde iki paralel istegin
                //     ayni zaman araligina randevu eklemesi imkansizdir.
                var start = request.AppointmentDate;
                var durationMinutes = service.DurationInMinutes;
                var end = start.AddMinutes(durationMinutes);
                var now = _dateTimeService.UtcNow;

                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var hasConflict = await _bookingGuard.HasConflictAsync(
                        request.EmployeeId, start, end, durationMinutes, token);

                    if (hasConflict)
                        return Result<Guid>.Failure(ConcurrencyErrors.BookingConflict());

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
                            request.IdempotencyKey, request.UserId, appointment.Id, now);

                        await _idempotencyRepository.AddAsync(record, token);
                    }

                    return Result<Guid>.Success(appointment.Id);
                }, IsolationLevel.Serializable, cancellationToken);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Randevu olusturulurken bir hata olustu.", ex.Message));
            }
        }

        // ===== YETKI KONTROLU =====
        private Result CheckAuthorization(
            CreateAppointmentCommand request,
            Business business)
        {
            // KURAL 1: Requester = UserId (kendine randevu)
            if (request.RequesterId == request.UserId)
                return Result.Success();

            // KURAL 2: Isletme sahibi calisanina randevu olusturuyor
            if (business.OwnerId == request.RequesterId)
                return Result.Success();

            // KURAL 3: Baska birine randevu olusturma yetkisi yok
            return Result.Failure(
                Error.Unauthorized("Baska bir kullanici adina randevu olusturma yetkiniz yok."));
        }

        // ===== 3. AKTIF RANDEVU KONTROLU =====
        private async Task<int> CountActiveAppointmentsAsync(
            Guid userId,
            Guid businessId,
            CancellationToken cancellationToken)
        {
            // Aktif = tarihi gelmemis, iptal edilmemis, tamamlanmamis
            var appointments = await _appointmentRepository.FindAsync(
                a => a.UserId == userId &&
                     a.BusinessId == businessId &&
                     a.Status != EnumAppointmentStatus.Cancelled &&
                     a.Status != EnumAppointmentStatus.Completed &&
                     a.AppointmentDate > _dateTimeService.UtcNow &&
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
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}
