using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.Businesses.Queries.SearchBusinesses
{
    public class SearchBusinessesQueryHandler : IRequestHandler<SearchBusinessesQuery, Result<List<BusinessDto>>>
    {
        private readonly IRepository<Business> _businessRepository;

        public SearchBusinessesQueryHandler(IRepository<Business> businessRepository)
        {
            _businessRepository = businessRepository;
        }

        public async Task<Result<List<BusinessDto>>> Handle(SearchBusinessesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Temel filtre: aktif, dogrulanmis, silinmemis
                Expression<Func<Business, bool>> predicate = b =>
                    b.IsActive &&
                    b.IsVerified &&
                    !b.IsDeleted;

                // 2. Kategori filtresi (opsiyonel)
                if (request.CategoryId.HasValue)
                {
                    var categoryId = request.CategoryId.Value;
                    predicate = predicate.AndAlso(b => b.CategoryId == categoryId);
                }

                // 3. Sehir filtresi (zorunlu)
                var city = request.City.Trim().ToLower();
                predicate = predicate.AndAlso(b => b.Address.City.ToLower() == city);

                // 4. Ilce filtresi (opsiyonel)
                if (!string.IsNullOrWhiteSpace(request.District))
                {
                    var district = request.District.Trim().ToLower();
                    predicate = predicate.AndAlso(b => b.Address.District.ToLower() == district);
                }

                // 5. Verileri getir
                var businesses = await _businessRepository.FindAsync(predicate, cancellationToken);

                // 6. Siralama ve sayfalama
                var pagedBusinesses = businesses
                    .OrderByDescending(b => b.Rating)
                    .ThenBy(b => b.Name)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 7. DTO'ya donustur
                var dtos = pagedBusinesses.Select(b => new BusinessDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    BusinessType = b.BusinessType,
                    Address = b.Address?.GetFullAddress() ?? string.Empty,
                    PhoneNumber = b.PhoneNumber?.Value ?? string.Empty,
                    Email = b.Email?.Value ?? string.Empty,
                    WebsiteUrl = b.WebsiteUrl,
                    LogoUrl = b.LogoUrl,
                    IsActive = b.IsActive,
                    IsVerified = b.IsVerified,
                    Rating = b.Rating,
                    TotalReviews = b.TotalReviews,
                    OwnerId = b.OwnerId,
                    CreatedAt = b.CreatedAt
                }).ToList();

                return Result<List<BusinessDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<BusinessDto>>.Failure(
                    Error.Internal("Isletmeler aranirken bir hata olustu.", ex.Message));
            }
        }
    }
}