using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Services.Queries.GetMyServices
{
    /// <summary>
    /// İşletme sahibinin hizmet listesi. Sahiplik OwnerScope ile
    /// doğrulanır; liste yalnızca istenen işletmenin hizmetlerini döner.
    /// </summary>
    public class GetMyServicesQueryHandler
        : IRequestHandler<GetMyServicesQuery, Result<AdminPagedResult<OwnerServiceListItemDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyServicesQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerServiceListItemDto>>> Handle(
            GetMyServicesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: işletme JWT sahibine ait olmalı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerServiceListItemDto>>.Failure(owned.Error!);

                // 2. Sayfalama kuralları.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
                var isActive = request.IsActive;

                // 3. Filtre: işletme + durum + serbest metin (ad).
                var services = await _serviceRepository.FindAsync(
                    s => s.BusinessId == businessId
                        && !s.IsDeleted
                        && (search == null || s.Name.Contains(search))
                        && (isActive == null || s.IsActive == isActive),
                    cancellationToken);

                // 4. Kararlı sıralama: önce ad, sonra Id.
                var ordered = services
                    .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(s => s.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new OwnerServiceListItemDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Price = s.Price?.Amount,
                        Currency = s.Price?.Currency,
                        DurationMinutes = s.DurationInMinutes,
                        IsActive = s.IsActive
                    })
                    .ToList();

                return Result<AdminPagedResult<OwnerServiceListItemDto>>.Success(
                    new AdminPagedResult<OwnerServiceListItemDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerServiceListItemDto>>.Failure(
                    Error.Internal("Hizmet listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
