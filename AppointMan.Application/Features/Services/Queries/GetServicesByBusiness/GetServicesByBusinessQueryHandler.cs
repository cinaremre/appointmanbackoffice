using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.Services.Queries.GetServicesByBusiness
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
                // 1. Filtreleme
                Expression<Func<Service, bool>> predicate = s => s.BusinessId == request.BusinessId && !s.IsDeleted;

                if (request.IsActive.HasValue)
                {
                    predicate = predicate.AndAlso(s => s.IsActive == request.IsActive.Value);
                }

                // 2. Verileri getir
                var services = await _serviceRepository.FindAsync(predicate, cancellationToken);

                // 3. Sayfalama
                var pagedServices = services
                    .OrderBy(s => s.Name)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 4. DTO'ya donustur
                var dtos = pagedServices.Select(s => new ServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Price = s.Price.Amount,
                    Currency = s.Price.Currency,
                    DurationInMinutes = s.DurationInMinutes,
                    Category = s.Category,
                    IsActive = s.IsActive,
                    BusinessId = s.BusinessId,
                    CreatedAt = s.CreatedAt
                }).ToList();

                return Result<List<ServiceDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<ServiceDto>>.Failure(
                    Error.Internal("Hizmetler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}