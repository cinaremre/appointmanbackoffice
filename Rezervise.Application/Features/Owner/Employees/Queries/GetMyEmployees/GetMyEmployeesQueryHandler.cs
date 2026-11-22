using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Employees.Queries.GetMyEmployees
{
    /// <summary>
    /// Isletme sahibinin calisan listesi. Sahiplik OwnerScope ile
    /// dogrulanir; liste yalnizca istenen isletmenin calisanlarini doner.
    /// </summary>
    public class GetMyEmployeesQueryHandler
        : IRequestHandler<GetMyEmployeesQuery, Result<AdminPagedResult<OwnerEmployeeListItemDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyEmployeesQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Employee> employeeRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerEmployeeListItemDto>>> Handle(
            GetMyEmployeesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: isletme JWT sahibine ait olmali.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerEmployeeListItemDto>>.Failure(owned.Error!);

                // 2. Sayfalama kurallari.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                // 3. Isletmenin silinmemis calisanlari.
                var employees = await _employeeRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId && !e.IsDeleted,
                    cancellationToken);

                // 4. Kararli siralama: once goruntuleme sirasi, sonra ad.
                var ordered = employees
                    .OrderBy(e => e.DisplayOrder)
                    .ThenBy(e => e.LastName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(e => e.FirstName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(e => e.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new OwnerEmployeeListItemDto
                    {
                        Id = e.Id,
                        FirstName = e.FirstName,
                        LastName = e.LastName,
                        PhoneNumber = e.PhoneNumber?.Value ?? string.Empty,
                        IsActive = e.IsActive
                    })
                    .ToList();

                return Result<AdminPagedResult<OwnerEmployeeListItemDto>>.Success(
                    new AdminPagedResult<OwnerEmployeeListItemDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerEmployeeListItemDto>>.Failure(
                    Error.Internal("Calisan listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
