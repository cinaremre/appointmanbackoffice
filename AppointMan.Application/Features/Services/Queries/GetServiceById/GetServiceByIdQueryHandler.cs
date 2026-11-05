using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Services.Queries.GetServiceById
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
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null)
                    return Result<ServiceDto>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                var dto = new ServiceDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Description = service.Description,
                    Price = service.Price.Amount,
                    Currency = service.Price.Currency,
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
                return Result<ServiceDto>.Failure(Error.Internal("Hizmet bilgileri getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}