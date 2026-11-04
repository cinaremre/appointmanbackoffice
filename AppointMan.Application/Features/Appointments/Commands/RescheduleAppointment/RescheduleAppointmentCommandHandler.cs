using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Commands.RescheduleAppointment
{
    public class RescheduleAppointmentCommandHandler : IRequestHandler<RescheduleAppointmentCommand, Result<Guid>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RescheduleAppointmentCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            IRepository<Employee> employeeRepository,
            IUnitOfWork unitOfWork)
        {
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _employeeRepository = employeeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Randevu kontrolu
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // 2. Yetki kontrolu
                if (appointment.UserId != request.UserId)
                    return Result<Guid>.Failure(Error.Unauthorized("Bu randevuyu yeniden planlama yetkiniz yok."));

                // 3. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(appointment.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", appointment.BusinessId.ToString()));

                if (!business.IsActive)
                    return Result<Guid>.Failure(Error.BusinessRule("Isletme aktif degil."));

                // 4. Calisan kontrolu (degisebilir veya ayni kalir)
                var employeeId = request.NewEmployeeId ?? appointment.EmployeeId;

                var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
                if (employee is null)
                    return Result<Guid>.Failure(Error.NotFound("Calisan", employeeId.ToString()));

                if (!employee.IsActive)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan aktif degil."));

                if (!employee.IsAvailableForBooking)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan su anda randevu kabul etmiyor."));

                if (employee.BusinessId != appointment.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu isletmeye ait degil."));

                // 5. Hizmet kontrolu
                var service = await _serviceRepository.GetByIdAsync(appointment.ServiceId, cancellationToken);
                if (service is null)
                    return Result<Guid>.Failure(Error.NotFound("Hizmet", appointment.ServiceId.ToString()));

                if (!service.IsActive)
                    return Result<Guid>.Failure(Error.BusinessRule("Hizmet aktif degil."));

                // 6. Calisan bu hizmeti verebiliyor mu?
                if (!employee.CanProvideService(appointment.ServiceId))
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu hizmeti veremiyor."));

                // 7. Calisma saati kontrolu
                if (!business.IsOpenOn(request.NewAppointmentDate))
                    return Result<Guid>.Failure(Error.BusinessRule("Isletme bu saatte calismiyor."));

                if (!employee.IsWorkingOn(request.NewAppointmentDate))
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu saatte calismiyor."));

                // 8. Calisan izinde mi?
                if (employee.IsOnTimeOff(request.NewAppointmentDate))
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu tarihte izinde."));

                // 9. Cakisma kontrolu (calisan bazli)
                var hasConflict = await CheckForConflicts(
                    appointment,
                    employeeId,
                    request.NewAppointmentDate,
                    service,
                    cancellationToken);

                if (hasConflict)
                    return Result<Guid>.Failure(Error.Conflict("Calisanin bu saatte baska bir randevusu var."));

                // 10. Randevuyu yeniden planla
                appointment.Reschedule(request.NewAppointmentDate, request.RescheduledBy ?? "User");

                // 11. Bitis saati guncelle
                var endTime = request.NewAppointmentDate.AddMinutes(service.DurationInMinutes);
                appointment.SetEndTime(endTime);

                // 12. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(appointment.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Randevu yeniden planlanirken bir hata olustu.", ex.Message));
            }
        }

        private async Task<bool> CheckForConflicts(
            Appointment appointment,
            Guid employeeId,
            DateTime newDate,
            Service service,
            CancellationToken cancellationToken)
        {
            var startTime = newDate;
            var endTime = startTime.AddMinutes(service.DurationInMinutes);

            var existingAppointments = await _appointmentRepository.FindAsync(
                a => a.EmployeeId == employeeId &&
                     a.Id != appointment.Id &&
                     a.Status != Domain.Enums.EnumAppointmentStatus.Cancelled &&
                     a.AppointmentDate < endTime &&
                     a.AppointmentDate.AddMinutes(a.Service.DurationInMinutes) > startTime,
                cancellationToken);

            return existingAppointments.Any();
        }
    }
}