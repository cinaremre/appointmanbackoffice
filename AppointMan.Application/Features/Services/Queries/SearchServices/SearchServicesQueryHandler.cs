using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.Services.Queries.SearchServices
{
    public class SearchServicesQueryHandler : IRequestHandler<SearchServicesQuery, Result<List<ServiceDto>>>
    {
        private readonly IRepository<Service> _serviceRepository;

        public SearchServicesQueryHandler(IRepository<Service> serviceRepository)
        {
            _serviceRepository = serviceRepository;
        }

        public async Task<Result<List<ServiceDto>>> Handle(SearchServicesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Temel filtre
                Expression<Func<Service, bool>> predicate = s => !s.IsDeleted;

                // 2. Isletme filtresi (opsiyonel)
                if (request.BusinessId.HasValue)
                {
                    var businessId = request.BusinessId.Value;
                    predicate = predicate.AndAlso(s => s.BusinessId == businessId);
                }

                // 3. Kategori filtresi
                if (request.Category.HasValue)
                {
                    var category = request.Category.Value;
                    predicate = predicate.AndAlso(s => s.Category == category);
                }

                // 4. Aktiflik filtresi
                if (request.IsActive.HasValue)
                {
                    var isActive = request.IsActive.Value;
                    predicate = predicate.AndAlso(s => s.IsActive == isActive);
                }

                // 5. Arama terimi
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim().ToLower();
                    predicate = predicate.AndAlso(s =>
                        s.Name.ToLower().Contains(term) ||
                        (s.Description != null && s.Description.ToLower().Contains(term)));
                }

                // 6. Verileri getir
                var services = await _serviceRepository.FindAsync(predicate, cancellationToken);

                // 7. Siralama ve sayfalama
                var pagedServices = services
                    .OrderBy(s => s.Name)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 8. DTO'ya donustur
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
                    BusinessName = s.Business?.Name,
                    BusinessAddress = s.Business?.Address?.GetFullAddress(),
                    CreatedAt = s.CreatedAt
                }).ToList();

                return Result<List<ServiceDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<ServiceDto>>.Failure(
                    Error.Internal("Hizmetler aranirken bir hata olustu.", ex.Message));
            }
        }
    }
}