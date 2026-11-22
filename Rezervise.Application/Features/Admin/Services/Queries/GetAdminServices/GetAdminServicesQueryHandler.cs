using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Services.Queries.GetAdminServices
{
    public class GetAdminServicesQueryHandler
        : IRequestHandler<GetAdminServicesQuery, Result<AdminServiceListResultDto>>
    {
        private readonly IRepository<Service> _serviceRepository;

        public GetAdminServicesQueryHandler(IRepository<Service> serviceRepository)
        {
            _serviceRepository = serviceRepository;
        }

        public async Task<Result<AdminServiceListResultDto>> Handle(
            GetAdminServicesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var serviceCategoryId = request.ServiceCategoryId;
                var search = string.IsNullOrWhiteSpace(request.Search)
                    ? null
                    : request.Search.Trim();
                var isActive = request.IsActive;

                var services = await _serviceRepository.FindAsync(
                    s => !s.IsDeleted
                        && (!businessId.HasValue || s.BusinessId == businessId.Value)
                        && (!serviceCategoryId.HasValue || s.ServiceCategoryId == serviceCategoryId.Value)
                        && (search == null || s.Name.Contains(search))
                        && (!isActive.HasValue || s.IsActive == isActive.Value),
                    cancellationToken);

                // Kararli sayfalama: once ad, sonra Id sirasi.
                var ordered = services
                    .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(s => s.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new AdminServiceListItemDto
                    {
                        Id = s.Id,
                        BusinessId = s.BusinessId,
                        Name = s.Name,
                        Price = s.Price?.Amount,
                        Currency = s.Price?.Currency,
                        DurationMinutes = s.DurationInMinutes,
                        IsActive = s.IsActive
                    })
                    .ToList();

                var result = new AdminServiceListResultDto
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = ordered.Count
                };

                return Result<AdminServiceListResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<AdminServiceListResultDto>.Failure(
                    Error.Internal("Hizmet listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
