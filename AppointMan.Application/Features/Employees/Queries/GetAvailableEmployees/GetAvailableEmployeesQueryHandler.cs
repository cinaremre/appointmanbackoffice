using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Employees.Queries.GetAvailableEmployees
{
    public class GetAvailableEmployeesQueryHandler : IRequestHandler<GetAvailableEmployeesQuery, Result<List<EmployeeDto>>>
    {
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Service> _serviceRepository;

        public GetAvailableEmployeesQueryHandler(
            IRepository<Employee> employeeRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<Service> serviceRepository)
        {
            _employeeRepository = employeeRepository;
            _appointmentRepository = appointmentRepository;
            _serviceRepository = serviceRepository;
        }

        public async Task<Result<List<EmployeeDto>>> Handle(GetAvailableEmployeesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletmenin aktif ve randevu kabul eden calisanlarini getir
                var employees = await _employeeRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId &&
                         e.IsActive &&
                         e.IsAvailableForBooking &&
                         !e.IsDeleted,
                    cancellationToken);

                var availableEmployees = employees.ToList();

                // 2. Hizmet filtresi
                if (request.ServiceId.HasValue)
                {
                    availableEmployees = availableEmployees
                        .Where(e => e.CanProvideService(request.ServiceId.Value))
                        .ToList();
                }

                // 3. Tarih filtresi - calisma saati, izin ve cakisma kontrolu
                if (request.AppointmentDate.HasValue && request.ServiceId.HasValue)
                {
                    var service = await _serviceRepository.GetByIdAsync(request.ServiceId.Value, cancellationToken);
                    if (service is null)
                        return Result<List<EmployeeDto>>.Failure(Error.NotFound("Hizmet", request.ServiceId.Value.ToString()));

                    var appointmentDate = request.AppointmentDate.Value;
                    var startTime = appointmentDate;
                    var endTime = startTime.AddMinutes(service.DurationInMinutes);

                    var filteredEmployees = new List<Employee>();

                    foreach (var employee in availableEmployees)
                    {
                        // Calisma saati kontrolu
                        if (!employee.IsWorkingOn(appointmentDate))
                            continue;

                        // Izin kontrolu
                        if (employee.IsOnTimeOff(appointmentDate))
                            continue;

                        // Cakisma kontrolu
                        var conflicts = await _appointmentRepository.FindAsync(
                            a => a.EmployeeId == employee.Id &&
                                 a.Status != Domain.Enums.EnumAppointmentStatus.Cancelled &&
                                 a.AppointmentDate < endTime &&
                                 a.AppointmentDate.AddMinutes(a.Service.DurationInMinutes) > startTime,
                            cancellationToken);

                        if (!conflicts.Any())
                            filteredEmployees.Add(employee);
                    }

                    availableEmployees = filteredEmployees;
                }

                // 4. DTO'ya donustur
                var dtos = availableEmployees
                    .OrderBy(e => e.DisplayOrder)
                    .Select(e => new EmployeeDto
                    {
                        Id = e.Id,
                        FirstName = e.FirstName,
                        LastName = e.LastName,
                        Title = e.Title,
                        Bio = e.Bio,
                        ProfilePictureUrl = e.ProfilePictureUrl,
                        PhoneNumber = e.PhoneNumber.Value,
                        Email = e.Email?.Value,
                        BusinessId = e.BusinessId,
                        BusinessName = e.Business?.Name,
                        IsActive = e.IsActive,
                        IsAvailableForBooking = e.IsAvailableForBooking,
                        DisplayOrder = e.DisplayOrder,
                        CreatedAt = e.CreatedAt,
                        FullName = e.GetFullName(),
                        DisplayName = e.GetDisplayName()
                    })
                    .ToList();

                return Result<List<EmployeeDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<EmployeeDto>>.Failure(
                    Error.Internal("Musait calisanlar getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}