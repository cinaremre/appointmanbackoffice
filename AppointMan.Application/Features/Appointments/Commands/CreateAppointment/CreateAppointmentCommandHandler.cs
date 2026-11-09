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
        private readonly IUnitOfWork _unitOfWork;

        public CreateAppointmentCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            IRepository<Employee> employeeRepository,
            IRepository<User> userRepository,
            IRepository<BlacklistEntry> blacklistRepository,
            IUnitOfWork unitOfWork)
        {
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _employeeRepository = employeeRepository;
            _userRepository = userRepository;
            _blacklistRepository = blacklistRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
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

                // 9. Cakisma kontrolu
                var hasConflict = await CheckForConflicts(request, service, cancellationToken);
                if (hasConflict)
                    return Result<Guid>.Failure(Error.Conflict("Calisanin bu saatte baska bir randevusu var."));

                // ===== 10. KARA LISTE KONTROLU =====
                var isBlacklisted = await _blacklistRepository.ExistsAsync(
                    b => b.UserId == request.UserId &&
                         b.BusinessId == request.BusinessId &&
                         !b.IsDeleted,
                    cancellationToken);

                if (isBlacklisted)
                    return Result<Guid>.Failure(
                        Error.BusinessRule("Randevunuz oluşturulamıyor, lütfen işletme ile iletişime geçiniz."));

                // ===== 11. 3. AKTIF RANDEVU KONTROLU =====
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

                // 12. Domain entity olustur
                var appointment = new Appointment(
                    request.UserId,
                    request.BusinessId,
                    request.ServiceId,
                    request.EmployeeId,
                    request.AppointmentDate,
                    request.Notes);

                // 13. Bitis saati hesapla
                var endTime = request.AppointmentDate.AddMinutes(service.DurationInMinutes);
                appointment.SetEndTime(endTime);

                // 14. Kaydet
                await _appointmentRepository.AddAsync(appointment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(appointment.Id);
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

        private async Task<bool> CheckForConflicts(
            CreateAppointmentCommand request,
            Service service,
            CancellationToken cancellationToken)
        {
            var startTime = request.AppointmentDate;
            var endTime = startTime.AddMinutes(service.DurationInMinutes);

            var existingAppointments = await _appointmentRepository.FindAsync(
                a => a.EmployeeId == request.EmployeeId &&
                     a.Status != EnumAppointmentStatus.Cancelled &&
                     a.AppointmentDate < endTime &&
                     a.AppointmentDate.AddMinutes(a.Service.DurationInMinutes) > startTime,
                cancellationToken);

            return existingAppointments.Any();
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
                     a.AppointmentDate > DateTime.UtcNow &&
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