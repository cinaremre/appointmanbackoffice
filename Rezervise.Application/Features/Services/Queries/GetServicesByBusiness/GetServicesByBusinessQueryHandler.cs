using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace Rezervise.Application.Features.Services.Queries.GetServicesByBusiness
{
    public class GetServicesByBusinessQueryHandler : IRequestHandler<GetServicesByBusinessQuery, Result<List<ServiceDto>>>
    {
        private readonly IRepository<Service> _serviceRepository;

        public GetServicesByBusinessQueryHandler(IRepository<Service> serviceRepository)
        {
            _serviceRepository = serviceRepository;
        }

        public async Task<Result<List<ServiceDto>>> Handle(GetServicesByBusinessQuery request, CancellationToken cancellationToken)
        {
            try
            {
                Expression<Func<Service, bool>> predicate = s => s.BusinessId == request.BusinessId && !s.IsDeleted;

                if (request.IsActive.HasValue)
                {
                    predicate = predicate.AndAlso(s => s.IsActive == request.IsActive.Value);
                }

                // Navigation'lar yüklenmezse ServiceCategoryName "Unknown", BusinessName ve
                // BusinessAddress null döner. GetName() çevirilere bağlı öldüğü için
                // Translations da include edilir. Address owned type öldüğü için
                // Business yüklendiğinde otomatik gelir.
                var services = await _serviceRepository.FindWithIncludesAsync(
                    predicate,
                    new[]
                    {
                        nameof(Service.ServiceCategory),
                        "ServiceCategory.Translations",
                        nameof(Service.Business)
                    },
                    cancellationToken);

                var pagedServices = services
                    .OrderBy(s => s.Name)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var dtos = pagedServices.Select(s => new ServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    //Price = s.Price.Amount,
                    //Currency = s.Price.Currency,
                    DurationInMinutes = s.DurationInMinutes,
                    ServiceCategoryId = s.ServiceCategoryId,
                    ServiceCategoryName = s.ServiceCategory?.GetName("tr"),
                    IsActive = s.IsActive,
                    BusinessId = s.BusinessId,
                    BusinessName = s.Business?.Name,
                    BusinessAddress = s.Business?.Address?.GetFullAddress(),
                    CreatedAt = s.CreatedAt
                }).ToList();

                return Result<List<ServiceDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<ServiceDto>>.Failure(
                    Error.Internal("Hizmetler getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}