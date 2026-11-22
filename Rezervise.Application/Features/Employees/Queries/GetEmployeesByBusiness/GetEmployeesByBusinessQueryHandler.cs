using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace Rezervise.Application.Features.Employees.Queries.GetEmployeesByBusiness
{
    public class GetEmployeesByBusinessQueryHandler : IRequestHandler<GetEmployeesByBusinessQuery, Result<List<EmployeeDto>>>
    {
        private readonly IRepository<Employee> _employeeRepository;

        public GetEmployeesByBusinessQueryHandler(IRepository<Employee> employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<Result<List<EmployeeDto>>> Handle(GetEmployeesByBusinessQuery request, CancellationToken cancellationToken)
        {
            try
            {
                Expression<Func<Employee, bool>> predicate = e => e.BusinessId == request.BusinessId && !e.IsDeleted;

                if (request.IsActive.HasValue)
                {
                    predicate = predicate.AndAlso(e => e.IsActive == request.IsActive.Value);
                }

                // Business navigasyonu yuklenmezse DTO'daki BusinessName her zaman null kalir.
                var employees = await _employeeRepository.FindWithIncludesAsync(
                    predicate,
                    new[] { nameof(Employee.Business) },
                    cancellationToken);

                var pagedEmployees = employees
                    .OrderBy(e => e.DisplayOrder)
                    .ThenBy(e => e.FirstName)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var dtos = pagedEmployees.Select(e => new EmployeeDto
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
                }).ToList();

                return Result<List<EmployeeDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<EmployeeDto>>.Failure(
                    Error.Internal("Calisanlar getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}