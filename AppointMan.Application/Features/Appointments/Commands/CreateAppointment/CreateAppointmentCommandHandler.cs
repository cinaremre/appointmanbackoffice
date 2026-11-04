using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
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
        private readonly IUnitOfWork _unitOfWork;

        public CreateAppointmentCommandHandler(
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

        public async Task<Result<Guid>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                if (!business.IsActive)
                    return Result<Guid>.Failure(Error.BusinessRule("Isletme aktif degil."));

                // 2. Hizmet kontrolu
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null)
                    return Result<Guid>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                if (!service.IsActive)
                    return Result<Guid>.Failure(Error.BusinessRule("Hizmet aktif degil."));

                if (service.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Hizmet bu isletmeye ait degil."));

                // 3. Calisan kontrolu
                var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
                if (employee is null)
                    return Result<Guid>.Failure(Error.NotFound("Calisan", request.EmployeeId.ToString()));

                if (!employee.IsActive)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan aktif degil."));

                if (!employee.IsAvailableForBooking)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan su anda randevu kabul etmiyor."));

                if (employee.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu isletmeye ait degil."));

                // 4. Calisan bu hizmeti verebiliyor mu?
                if (!employee.CanProvideService(request.ServiceId))
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu hizmeti veremiyor."));

                // 5. Calisma saati kontrolu (once isletme, sonra calisan)
                if (!business.IsOpenOn(request.AppointmentDate))
                    return Result<Guid>.Failure(Error.BusinessRule("Isletme bu saatte calismiyor."));

                if (!employee.IsWorkingOn(request.AppointmentDate))
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu saatte calismiyor."));

                // 6. Calisan izinde mi?
                if (employee.IsOnTimeOff(request.AppointmentDate))
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu tarihte izinde."));

                // 7. Cakisma kontrolu (calisan bazli)
                var hasConflict = await CheckForConflicts(request, cancellationToken);
                if (hasConflict)
                    return Result<Guid>.Failure(Error.Conflict("Calisanin bu saatte baska bir randevusu var."));

                // 8. Domain entity olustur
                var appointment = new Appointment(
                    request.UserId,
                    request.BusinessId,
                    request.ServiceId,
                    request.EmployeeId,
                    request.AppointmentDate,
                    request.Notes);

                // 9. Bitis saati hesapla
                var duration = service.DurationInMinutes;
                var endTime = request.AppointmentDate.AddMinutes(duration);
                appointment.SetEndTime(endTime);

                // 10. Kaydet
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

        private async Task<bool> CheckForConflicts(CreateAppointmentCommand request, CancellationToken cancellationToken)
        {
            var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
            if (service is null)
                return true;

            var startTime = request.AppointmentDate;
            var endTime = startTime.AddMinutes(service.DurationInMinutes);

            var existingAppointments = await _appointmentRepository.FindAsync(
                a => a.EmployeeId == request.EmployeeId &&
                     a.Status != Domain.Enums.EnumAppointmentStatus.Cancelled &&
                     a.AppointmentDate < endTime &&
                     a.AppointmentDate.AddMinutes(a.Service.DurationInMinutes) > startTime,
                cancellationToken);

            return existingAppointments.Any();
        }
    }
}