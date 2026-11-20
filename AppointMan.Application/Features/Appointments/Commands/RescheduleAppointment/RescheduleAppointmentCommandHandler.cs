using System.Data;
using AppointMan.Application.Common.Abstractions;
using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Commands.RescheduleAppointment
{
    public class RescheduleAppointmentCommandHandler : IRequestHandler<RescheduleAppointmentCommand, Result<Guid>>
    {
        private const string BlacklistMessage = "Randevunuz oluşturulamıyor, lütfen işletme ile iletişime geçiniz.";

        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<BlacklistEntry> _blacklistRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeService _dateTimeService;
        private readonly IAppointmentBookingGuard _bookingGuard;
        private readonly AppointManMetrics _metrics;
        private readonly ICurrentUserService _currentUser;

        public RescheduleAppointmentCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            IRepository<Employee> employeeRepository,
            IRepository<BlacklistEntry> blacklistRepository,
            IUnitOfWork unitOfWork,
            IDateTimeService dateTimeService,
            IAppointmentBookingGuard bookingGuard,
            AppointManMetrics metrics,
            ICurrentUserService currentUser)
        {
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _employeeRepository = employeeRepository;
            _blacklistRepository = blacklistRepository;
            _unitOfWork = unitOfWork;
            _dateTimeService = dateTimeService;
            _bookingGuard = bookingGuard;
            _metrics = metrics;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var now = _dateTimeService.UtcNow;

                // Butun kontroller ve mutation ayni SERIALIZABLE transaction icinde.
                var result = await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    // 1. Randevu kontrolu
                    var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, token);
                    if (appointment is null)
                        return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                    // 2. Yetki kontrolu - randevu sahibi veya yonetici.
                    if (!CallerAuthorization.CanActAsUser(_currentUser, appointment.UserId))
                        return Result<Guid>.Failure(Error.Forbidden("Bu randevuyu yeniden planlama yetkiniz yok."));

                    // 3. Isletme kontrolu
                    var business = await _businessRepository.GetByIdWithIncludesAsync(
                        appointment.BusinessId,
                        token,
                        b => b.WorkingHours);

                    if (business is null)
                        return Result<Guid>.Failure(Error.NotFound("Isletme", appointment.BusinessId.ToString()));

                    if (!business.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("Isletme aktif degil."));

                    // 4. Calisan kontrolu (degisebilir veya ayni kalir)
                    var employeeId = request.NewEmployeeId ?? appointment.EmployeeId;

                    var employee = await _employeeRepository.GetByIdWithIncludesAsync(
                        employeeId,
                        token,
                        e => e.EmployeeServices,
                        e => e.WorkingHours,
                        e => e.TimeOffs);

                    if (employee is null)
                        return Result<Guid>.Failure(Error.NotFound("Calisan", employeeId.ToString()));

                    if (!employee.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("Calisan aktif degil."));

                    if (!employee.IsAvailableForBooking)
                        return Result<Guid>.Failure(Error.BusinessRule("Calisan su anda randevu kabul etmiyor."));

                    if (employee.BusinessId != appointment.BusinessId)
                        return Result<Guid>.Failure(Error.BusinessRule("Calisan bu isletmeye ait degil."));

                    // 5. Hizmet kontrolu
                    var service = await _serviceRepository.GetByIdAsync(appointment.ServiceId, token);
                    if (service is null)
                        return Result<Guid>.Failure(Error.NotFound("Hizmet", appointment.ServiceId.ToString()));

                    if (!service.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("Hizmet aktif degil."));

                    // 6. Calisan bu hizmeti verebiliyor mu?
                    if (!employee.CanProvideService(appointment.ServiceId))
                        return Result<Guid>.Failure(Error.BusinessRule("Calisan bu hizmeti veremiyor."));

                    // 7. Calisma saati kontrolu - UTC anini isletme yerel zamanina cevir.
                    var start = request.NewAppointmentDate;
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

                    // 8. Cakisma kontrolu. Domain overlap formulu:
                    //    appointment.EndTime ?? appointment.AppointmentDate.AddMinutes(service.DurationInMinutes)
                    //    DB tarafinda a.Service.DurationInMinutes kullanilmadan fallback sure ile uygulanir.
                    var hasConflict = await _bookingGuard.HasConflictAsync(
                        employeeId,
                        start,
                        end,
                        durationMinutes,
                        excludeAppointmentId: appointment.Id,
                        cancellationToken: token);

                    if (hasConflict)
                        return Result<Guid>.Failure(Error.Conflict("Calisanin bu saatte baska bir randevusu var."));

                    // 9. Kara liste tekrar kontrolu (bu islemde yeni kayit olusturulmaz).
                    if (await IsBlacklistedAsync(request.UserId, appointment.BusinessId, token))
                        return Result<Guid>.Failure(Error.BusinessRule(BlacklistMessage));

                    // 10. Aktif randevu limiti, yeniden planlanan randevu haric.
                    var activeAppointmentCount = await CountActiveAppointmentsAsync(
                        request.UserId, appointment.BusinessId, appointment.Id, now, token);

                    if (activeAppointmentCount >= 2)
                        return Result<Guid>.Failure(
                            Error.BusinessRule("Aktif randevu limiti nedeniyle yeniden planlanamaz."));

                    // 11. Calisan degisikligi kalici uygulanir; ayni calisansa domain no-op yapar.
                    var changedBy = request.RescheduledBy ?? "User";
                    if (request.NewEmployeeId.HasValue)
                        appointment.ReassignEmployee(request.NewEmployeeId.Value, changedBy, now);

                    // 12. Randevuyu yeni tarihe tasir ve bitis saatini servis suresinden yazar.
                    appointment.Reschedule(start, changedBy, now);
                    appointment.SetEndTime(end);

                    return Result<Guid>.Success(appointment.Id);
                }, IsolationLevel.Serializable, cancellationToken);

                if (result.IsSuccess)
                    _metrics.RecordAppointmentRescheduled();

                return result;
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(Error.Internal("Randevu yeniden planlanirken bir hata olustu."));
            }
        }

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

        private async Task<int> CountActiveAppointmentsAsync(
            Guid userId,
            Guid businessId,
            Guid excludeAppointmentId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var appointments = await _appointmentRepository.FindAsync(
                a => a.UserId == userId &&
                     a.BusinessId == businessId &&
                     a.Id != excludeAppointmentId &&
                     a.Status != EnumAppointmentStatus.Cancelled &&
                     a.Status != EnumAppointmentStatus.Completed &&
                     a.AppointmentDate > now &&
                     !a.IsDeleted,
                cancellationToken);

            return appointments.Count;
        }
    }
}
