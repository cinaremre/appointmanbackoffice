using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace Rezervise.Application.Features.Businesses.Queries.SearchBusinesses
{
    public class SearchBusinessesQueryHandler : IRequestHandler<SearchBusinessesQuery, Result<List<BusinessDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<City> _cityRepository;

        public SearchBusinessesQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<User> userRepository,
            IRepository<City> cityRepository)
        {
            _businessRepository = businessRepository;
            _userRepository = userRepository;
            _cityRepository = cityRepository;
        }

        public async Task<Result<List<BusinessDto>>> Handle(SearchBusinessesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Temel filtre: aktif, doğrulanmış, ONAYLANMIŞ, silinmemiş.
                //    Onay bekleyen işletmeler backoffice onayına kadar listelenmez.
                Expression<Func<Business, bool>> predicate = b =>
                    b.IsActive &&
                    b.IsVerified &&
                    b.IsApproved &&
                    !b.IsDeleted;

                // 2. Kategori filtresi (opsiyonel)
                if (request.CategoryId.HasValue)
                {
                    var categoryId = request.CategoryId.Value;
                    predicate = predicate.AndAlso(b => b.CategoryId == categoryId);
                }

                // 3. Konum çözümleme: istekte gelen değer varsa o kullanılır ve ASLA ezilmez.
                //    İstekte konum gelmediyse kullanıcının kayıtlı adresi devreye girer.
                var cityId = request.CityId;
                var districtId = request.DistrictId;

                // İstekte şehir ADI geldiyse ID'ye çözülür. Address bir value object öldüğü ve
                // City navigasyonu içermediği için önce ID çözümü, sonra Address.CityId filtresi gerekir.
                // CityId doluysa ada hiç bakılmaz; sayısal ID her zaman öncelikli.
                if (!cityId.HasValue && !string.IsNullOrWhiteSpace(request.City))
                {
                    var rawCity = request.City.Trim();
                    var normalized = SearchNormalizer.Normalize(rawCity);
                    var lowered = rawCity.ToLower();

                    // 1) Tam eşleşme (normalize edilmiş arama adı veya birebir ad)
                    var citiesByName = await _cityRepository.FindAsync(
                        c => c.SearchName == normalized || c.Name.ToLower() == lowered,
                        cancellationToken);

                    var resolvedCity = citiesByName.FirstOrDefault();

                    // 2) Tam eşleşme yoksa kısmı eşleşmeye düş (orn. "Ankar" -> "Ankara")
                    if (resolvedCity is null)
                    {
                        var citiesByPartial = await _cityRepository.FindAsync(
                            c => c.SearchName.Contains(normalized) || c.Name.ToLower().Contains(lowered),
                            cancellationToken);

                        resolvedCity = citiesByPartial.FirstOrDefault();
                    }

                    if (resolvedCity is null)
                        return Result<List<BusinessDto>>.Failure(
                            Error.BusinessRule("Belirtilen şehir bulunamadı."));

                    cityId = resolvedCity.Id;
                }

                if (!cityId.HasValue && !districtId.HasValue && request.CurrentUserId.HasValue)
                {
                    var users = await _userRepository.FindAsync(
                        u => u.Id == request.CurrentUserId.Value, cancellationToken);

                    var user = users.FirstOrDefault();
                    if (user is not null)
                    {
                        cityId = user.CityId;
                        districtId = user.DistrictId;
                    }
                }

                if (!cityId.HasValue && !districtId.HasValue)
                    return Result<List<BusinessDto>>.Failure(
                        Error.BusinessRule("Arama için şehir veya ilçe bilgisi gereklidir."));

                if (cityId.HasValue)
                    predicate = predicate.AndAlso(b => b.Address.CityId == cityId.Value);

                // 4. İlçe filtresi (opsiyonel)
                if (districtId.HasValue)
                {
                    predicate = predicate.AndAlso(b => b.Address.DistrictId == districtId.Value);
                }

                // 5. Locality filtresi (opsiyonel)
                if (request.LocalityId.HasValue)
                {
                    predicate = predicate.AndAlso(b => b.Address.LocalityId == request.LocalityId.Value);
                }

                // 6. Mahalle filtresi (opsiyonel)
                if (request.NeighborhoodId.HasValue)
                {
                    predicate = predicate.AndAlso(b => b.Address.NeighborhoodId == request.NeighborhoodId.Value);
                }

                // 5. Verileri getir
                var businesses = await _businessRepository.FindAsync(predicate, cancellationToken);

                // 6. Sıralama ve sayfalama
                var pagedBusinesses = businesses
                    .OrderByDescending(b => b.Rating)
                    .ThenBy(b => b.Name)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 7. DTO'ya dönüştür
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
                    IsApproved = b.IsApproved,
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
                    Error.Internal("İşletmeler aranırken bir hata oluştu.", ex.Message));
            }
        }
    }
}