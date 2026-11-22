using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Employees.Queries.GetEmployeeById
{
    public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDto>>
    {
        private readonly IRepository<Employee> _employeeRepository;

        public GetEmployeeByIdQueryHandler(IRepository<Employee> employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<Result<EmployeeDto>> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Calisani ID'ye gore getir.
                //    Business navigasyonu yuklenmezse DTO'daki BusinessName her zaman null kalir.
                var matches = await _employeeRepository.FindWithIncludesAsync(
                    e => e.Id == request.EmployeeId && !e.IsDeleted,
                    new[] { nameof(Employee.Business) },
                    cancellationToken);

                var employee = matches.FirstOrDefault();

                // 2. Calisan bulunamadiysa hata dondur
                if (employee is null)
                    return Result<EmployeeDto>.Failure(Error.NotFound("Calisan", request.EmployeeId.ToString()));

                // 3. DTO'ya donustur
                var dto = new EmployeeDto
                {
                    Id = employee.Id,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Title = employee.Title,
                    Bio = employee.Bio,
                    ProfilePictureUrl = employee.ProfilePictureUrl,
                    PhoneNumber = employee.PhoneNumber.Value,
                    Email = employee.Email?.Value,
                    BusinessId = employee.BusinessId,
                    BusinessName = employee.Business?.Name,
                    IsActive = employee.IsActive,
                    IsAvailableForBooking = employee.IsAvailableForBooking,
                    DisplayOrder = employee.DisplayOrder,
                    CreatedAt = employee.CreatedAt,
                    FullName = employee.GetFullName(),
                    DisplayName = employee.GetDisplayName()
                };

                return Result<EmployeeDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<EmployeeDto>.Failure(Error.Internal("Calisan bilgileri getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}