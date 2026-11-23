using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Services.Queries.GetServiceById
{
    public class GetServiceByIdQueryHandler : IRequestHandler<GetServiceByIdQuery, Result<ServiceDto>>
    {
        private readonly IRepository<Service> _serviceRepository;

        public GetServiceByIdQueryHandler(IRepository<Service> serviceRepository)
        {
            _serviceRepository = serviceRepository;
        }

        public async Task<Result<ServiceDto>> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Navigation'lar yüklenmezse ServiceCategoryName "Unknown", BusinessName ve
                // BusinessAddress null döner. GetName() çevirilere bağlı öldüğü için
                // Translations da include edilir. Address owned type öldüğü için
                // Business yüklendiğinde otomatik gelir.
                var matches = await _serviceRepository.FindWithIncludesAsync(
                    s => s.Id == request.ServiceId && !s.IsDeleted,
                    new[]
                    {
                        nameof(Service.ServiceCategory),
                        "ServiceCategory.Translations",
                        nameof(Service.Business)
                    },
                    cancellationToken);

                var service = matches.FirstOrDefault();
                if (service is null)
                    return Result<ServiceDto>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                var dto = new ServiceDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Description = service.Description,
                    //Price = service.Price.Amount,
                    //Currency = service.Price.Currency,
                    DurationInMinutes = service.DurationInMinutes,
                    ServiceCategoryId = service.ServiceCategoryId,
                    ServiceCategoryName = service.ServiceCategory?.GetName("tr"),
                    IsActive = service.IsActive,
                    BusinessId = service.BusinessId,
                    BusinessName = service.Business?.Name,
                    BusinessAddress = service.Business?.Address?.GetFullAddress(),
                    CreatedAt = service.CreatedAt
                };

                return Result<ServiceDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<ServiceDto>.Failure(Error.Internal("Hizmet bilgileri getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}