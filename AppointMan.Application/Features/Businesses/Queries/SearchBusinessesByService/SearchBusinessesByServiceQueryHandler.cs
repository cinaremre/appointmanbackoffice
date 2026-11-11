using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.Businesses.Queries.SearchBusinessesByService
{
    public class SearchBusinessesByServiceQueryHandler
        : IRequestHandler<SearchBusinessesByServiceQuery, Result<List<BusinessDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<ServiceCategory> _serviceCategoryRepository;
        private readonly IRepository<User> _userRepository;

        public SearchBusinessesByServiceQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            IRepository<ServiceCategory> serviceCategoryRepository,
            IRepository<User> userRepository)
        {
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _serviceCategoryRepository = serviceCategoryRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<List<BusinessDto>>> Handle(
            SearchBusinessesByServiceQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Hizmet filtresi var mi?
                var hasServiceFilter = request.ServiceCategoryId.HasValue ||
                                       !string.IsNullOrWhiteSpace(request.ServiceSearchTerm);

                List<Guid>? matchingBusinessIds = null;

                // 2. Hizmet filtresi varsa service'leri bul
                if (hasServiceFilter)
                {
                    Expression<Func<Service, bool>> servicePredicate = s =>
                        s.IsActive && !s.IsDeleted;

                    // 2.1. Kategori filtresi
                    if (request.ServiceCategoryId.HasValue)
                    {
                        var categoryId = request.ServiceCategoryId.Value;
                        var category = await _serviceCategoryRepository.GetByIdAsync(categoryId, cancellationToken);

                        if (category is null)
                            return Result<List<BusinessDto>>.Failure(
                                Error.NotFound("Kategori", categoryId.ToString()));

                        // Kategori leaf ise sadece bu kategoriyi, degilse tum alt kategorileri al
                        if (category.IsLeaf())
                        {
                            servicePredicate = servicePredicate.AndAlso(s =>
                                s.ServiceCategoryId == categoryId);
                        }
                        else
                        {
                            var descendantIds = category.GetAllDescendants()
                                .Select(c => c.Id)
                                .Append(categoryId)
                                .ToList();

                            servicePredicate = servicePredicate.AndAlso(s =>
                                descendantIds.Contains(s.ServiceCategoryId));
                        }
                    }

                    // 2.2. Arama terimi
                    if (!string.IsNullOrWhiteSpace(request.ServiceSearchTerm))
                    {
                        var term = request.ServiceSearchTerm.Trim().ToLower();
                        servicePredicate = servicePredicate.AndAlso(s =>
                            s.Name.ToLower().Contains(term) ||
                            (s.Description != null && s.Description.ToLower().Contains(term)));
                    }

                    // 2.3. Hizmetleri getir
                    var services = await _serviceRepository.FindAsync(servicePredicate, cancellationToken);

                    if (!services.Any())
                        return Result<List<BusinessDto>>.Success(new List<BusinessDto>());

                    // 2.4. Bu hizmetleri veren isletmeleri bul
                    matchingBusinessIds = services
                        .Select(s => s.BusinessId)
                        .Distinct()
                        .ToList();
                }

                // 3. Isletme filtresi
                Expression<Func<Business, bool>> businessPredicate = b =>
                    b.IsActive &&
                    b.IsVerified &&
                    !b.IsDeleted;

                // 3.1. Isletme ID'leri (hizmet filtresinden)
                if (matchingBusinessIds != null)
                {
                    var ids = matchingBusinessIds;
                    businessPredicate = businessPredicate.AndAlso(b =>
                        ids.Contains(b.Id));
                }

                // 3.2. Isletme kategorisi
                if (request.BusinessCategoryId.HasValue)
                {
                    var businessCategoryId = request.BusinessCategoryId.Value;
                    businessPredicate = businessPredicate.AndAlso(b =>
                        b.CategoryId == businessCategoryId);
                }

                // 3.3. Konum cozumleme: istekte gelen deger varsa o kullanilir ve ASLA ezilmez.
                //      Istekte konum gelmediyse kullanicinin kayitli adresi devreye girer.
                var cityId = request.CityId;
                var districtId = request.DistrictId;

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
                        Error.BusinessRule("Arama icin sehir veya ilce bilgisi gereklidir."));

                if (cityId.HasValue)
                {
                    businessPredicate = businessPredicate.AndAlso(b =>
                        b.Address.CityId == cityId.Value);
                }

                // 3.4. Ilce (opsiyonel)
                if (districtId.HasValue)
                {
                    businessPredicate = businessPredicate.AndAlso(b =>
                        b.Address.DistrictId == districtId.Value);
                }

                // 3.5. Locality (opsiyonel)
                if (request.LocalityId.HasValue)
                {
                    businessPredicate = businessPredicate.AndAlso(b =>
                        b.Address.LocalityId == request.LocalityId.Value);
                }

                // 3.6. Mahalle (opsiyonel)
                if (request.NeighborhoodId.HasValue)
                {
                    businessPredicate = businessPredicate.AndAlso(b =>
                        b.Address.NeighborhoodId == request.NeighborhoodId.Value);
                }

                // 4. Isletmeleri getir
                var businesses = await _businessRepository.FindAsync(businessPredicate, cancellationToken);

                // 5. Siralama ve sayfalama
                var pagedBusinesses = businesses
                    .OrderByDescending(b => b.Rating)
                    .ThenBy(b => b.Name)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 6. DTO'ya donustur
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