using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.Autocomplete.Queries.Search
{
    /// <summary>
    /// Serves the unified autocomplete endpoint by dispatching on the requested
    /// source type. Address entities are matched through their normalised
    /// SearchName column; categories fall back to a case-insensitive Name match.
    /// Parent names are loaded in the same query through the parent navigation.
    /// </summary>
    public class AutocompleteQueryHandler : IRequestHandler<AutocompleteQuery, Result<List<AutocompleteItemDto>>>
    {
        private readonly IRepository<City> _cityRepository;
        private readonly IRepository<District> _districtRepository;
        private readonly IRepository<Locality> _localityRepository;
        private readonly IRepository<Neighborhood> _neighborhoodRepository;
        private readonly IRepository<BusinessCategory> _businessCategoryRepository;
        private readonly IRepository<ServiceCategory> _serviceCategoryRepository;

        public AutocompleteQueryHandler(
            IRepository<City> cityRepository,
            IRepository<District> districtRepository,
            IRepository<Locality> localityRepository,
            IRepository<Neighborhood> neighborhoodRepository,
            IRepository<BusinessCategory> businessCategoryRepository,
            IRepository<ServiceCategory> serviceCategoryRepository)
        {
            _cityRepository = cityRepository;
            _districtRepository = districtRepository;
            _localityRepository = localityRepository;
            _neighborhoodRepository = neighborhoodRepository;
            _businessCategoryRepository = businessCategoryRepository;
            _serviceCategoryRepository = serviceCategoryRepository;
        }

        public async Task<Result<List<AutocompleteItemDto>>> Handle(
            AutocompleteQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var term = request.Query.Trim();

                // SearchNormalizer, City.SearchName alanini ureten ayni arac; boylece
                // buyuk/kucuk harf ve Turkce karakter farklari esitlenir.
                var normalized = SearchNormalizer.Normalize(term);
                var lowered = term.ToLowerInvariant();
                var type = request.Type.Trim().ToLowerInvariant();

                switch (type)
                {
                    case "city":
                        return await SearchCitiesAsync(normalized, term, request.Limit, cancellationToken);

                    case "district":
                        return await SearchDistrictsAsync(normalized, term, request.ParentId, request.Limit, cancellationToken);

                    case "locality":
                        return await SearchLocalitiesAsync(normalized, term, request.ParentId, request.Limit, cancellationToken);

                    case "neighborhood":
                        return await SearchNeighborhoodsAsync(normalized, term, request.ParentId, request.Limit, cancellationToken);

                    case "businesscategory":
                        return await SearchBusinessCategoriesAsync(lowered, term, request.ParentId, request.Limit, cancellationToken);

                    case "servicecategory":
                        return await SearchServiceCategoriesAsync(lowered, term, request.ParentId, request.Limit, cancellationToken);

                    default:
                        return Result<List<AutocompleteItemDto>>.Failure(
                            Error.Validation("Desteklenmeyen arama tipi."));
                }
            }
            catch (Exception ex)
            {
                return Result<List<AutocompleteItemDto>>.Failure(
                    Error.Internal("Otomatik tamamlama sirasinda bir hata olustu.", ex.Message));
            }
        }

        /// <summary>Searches cities. Cities sit at the root, so parent filtering is ignored.</summary>
        private async Task<Result<List<AutocompleteItemDto>>> SearchCitiesAsync(
            string normalized,
            string term,
            int limit,
            CancellationToken cancellationToken)
        {
            var cities = await _cityRepository.FindAsync(
                c => c.SearchName.Contains(normalized),
                cancellationToken);

            var items = cities.Select(c => new AutocompleteItemDto
            {
                Id = c.Id.ToString(),
                Name = c.Name,
                Type = "city"
            }).ToList();

            return Result<List<AutocompleteItemDto>>.Success(OrderAndTrim(items, term, limit));
        }

        /// <summary>Searches districts, optionally scoped to a city.</summary>
        private async Task<Result<List<AutocompleteItemDto>>> SearchDistrictsAsync(
            string normalized,
            string term,
            string? parentId,
            int limit,
            CancellationToken cancellationToken)
        {
            if (!TryGetIntParent(parentId, out var cityId))
                return Result<List<AutocompleteItemDto>>.Failure(Error.Validation("Ust birim ID gecersiz."));

            Expression<Func<District, bool>> predicate = d => d.SearchName.Contains(normalized);

            if (cityId.HasValue)
            {
                var filterCityId = cityId.Value;
                predicate = predicate.AndAlso(d => d.CityId == filterCityId);
            }

            // Ust birim adi ayni sorguda gelsin diye City navigasyonu include edilir.
            var districts = await _districtRepository.FindWithIncludesAsync(
                predicate,
                new[] { nameof(District.City) },
                cancellationToken);

            var items = districts.Select(d => new AutocompleteItemDto
            {
                Id = d.Id.ToString(),
                Name = d.Name,
                Type = "district",
                ParentId = d.CityId.ToString(),
                ParentName = d.City?.Name
            }).ToList();

            return Result<List<AutocompleteItemDto>>.Success(OrderAndTrim(items, term, limit));
        }

        /// <summary>Searches localities, optionally scoped to a district.</summary>
        private async Task<Result<List<AutocompleteItemDto>>> SearchLocalitiesAsync(
            string normalized,
            string term,
            string? parentId,
            int limit,
            CancellationToken cancellationToken)
        {
            if (!TryGetIntParent(parentId, out var districtId))
                return Result<List<AutocompleteItemDto>>.Failure(Error.Validation("Ust birim ID gecersiz."));

            Expression<Func<Locality, bool>> predicate = l => l.SearchName.Contains(normalized);

            if (districtId.HasValue)
            {
                var filterDistrictId = districtId.Value;
                predicate = predicate.AndAlso(l => l.DistrictId == filterDistrictId);
            }

            var localities = await _localityRepository.FindWithIncludesAsync(
                predicate,
                new[] { nameof(Locality.District) },
                cancellationToken);

            var items = localities.Select(l => new AutocompleteItemDto
            {
                Id = l.Id.ToString(),
                Name = l.Name,
                Type = "locality",
                ParentId = l.DistrictId.ToString(),
                ParentName = l.District?.Name
            }).ToList();

            return Result<List<AutocompleteItemDto>>.Success(OrderAndTrim(items, term, limit));
        }

        /// <summary>Searches neighborhoods, optionally scoped to a locality.</summary>
        private async Task<Result<List<AutocompleteItemDto>>> SearchNeighborhoodsAsync(
            string normalized,
            string term,
            string? parentId,
            int limit,
            CancellationToken cancellationToken)
        {
            if (!TryGetIntParent(parentId, out var localityId))
                return Result<List<AutocompleteItemDto>>.Failure(Error.Validation("Ust birim ID gecersiz."));

            Expression<Func<Neighborhood, bool>> predicate = n => n.SearchName.Contains(normalized);

            if (localityId.HasValue)
            {
                var filterLocalityId = localityId.Value;
                predicate = predicate.AndAlso(n => n.LocalityId == filterLocalityId);
            }

            var neighborhoods = await _neighborhoodRepository.FindWithIncludesAsync(
                predicate,
                new[] { nameof(Neighborhood.Locality) },
                cancellationToken);

            var items = neighborhoods.Select(n => new AutocompleteItemDto
            {
                Id = n.Id.ToString(),
                Name = n.Name,
                Type = "neighborhood",
                ParentId = n.LocalityId.ToString(),
                ParentName = n.Locality?.Name
            }).ToList();

            return Result<List<AutocompleteItemDto>>.Success(OrderAndTrim(items, term, limit));
        }

        /// <summary>Searches business categories, optionally scoped to a parent category.</summary>
        private async Task<Result<List<AutocompleteItemDto>>> SearchBusinessCategoriesAsync(
            string lowered,
            string term,
            string? parentId,
            int limit,
            CancellationToken cancellationToken)
        {
            if (!TryGetGuidParent(parentId, out var parentCategoryId))
                return Result<List<AutocompleteItemDto>>.Failure(Error.Validation("Ust birim ID gecersiz."));

            // BusinessCategory'de SearchName yok; buyuk/kucuk harf duyarsizlik ToLower ile saglanir.
            Expression<Func<BusinessCategory, bool>> predicate = b => b.Name.ToLower().Contains(lowered);

            if (parentCategoryId.HasValue)
            {
                var filterParentId = parentCategoryId.Value;
                predicate = predicate.AndAlso(b => b.ParentCategoryId == filterParentId);
            }

            var categories = await _businessCategoryRepository.FindWithIncludesAsync(
                predicate,
                new[] { nameof(BusinessCategory.ParentCategory) },
                cancellationToken);

            var items = categories.Select(b => new AutocompleteItemDto
            {
                Id = b.Id.ToString(),
                Name = b.Name,
                Type = "businessCategory",
                ParentId = b.ParentCategoryId?.ToString(),
                ParentName = b.ParentCategory?.Name
            }).ToList();

            return Result<List<AutocompleteItemDto>>.Success(OrderAndTrim(items, term, limit));
        }

        /// <summary>
        /// Searches service categories, optionally scoped to a parent category.
        /// Names are localised through the translation collection.
        /// </summary>
        private async Task<Result<List<AutocompleteItemDto>>> SearchServiceCategoriesAsync(
            string lowered,
            string term,
            string? parentId,
            int limit,
            CancellationToken cancellationToken)
        {
            if (!TryGetGuidParent(parentId, out var parentCategoryId))
                return Result<List<AutocompleteItemDto>>.Failure(Error.Validation("Ust birim ID gecersiz."));

            // ServiceCategory'de Name alani yok; ad yalnizca ceviri koleksiyonunda tutulur.
            Expression<Func<ServiceCategory, bool>> predicate =
                c => c.Translations.Any(t => t.Name.ToLower().Contains(lowered));

            if (parentCategoryId.HasValue)
            {
                var filterParentId = parentCategoryId.Value;
                predicate = predicate.AndAlso(c => c.ParentCategoryId == filterParentId);
            }

            var categories = await _serviceCategoryRepository.FindWithIncludesAsync(
                predicate,
                new[]
                {
                    nameof(ServiceCategory.Translations),
                    nameof(ServiceCategory.ParentCategory),
                    "ParentCategory.Translations"
                },
                cancellationToken);

            var items = categories.Select(c => new AutocompleteItemDto
            {
                Id = c.Id.ToString(),
                Name = c.GetName("tr"),
                Type = "serviceCategory",
                ParentId = c.ParentCategoryId?.ToString(),
                ParentName = c.ParentCategory?.GetName("tr")
            }).ToList();

            return Result<List<AutocompleteItemDto>>.Success(OrderAndTrim(items, term, limit));
        }

        /// <summary>
        /// Onek eslesenleri one alir; otomatik tamamlamada en olasi secenek ustte olmali.
        /// Siralama bellekte yapilir, sonra limit uygulanir.
        /// </summary>
        private static List<AutocompleteItemDto> OrderAndTrim(
            List<AutocompleteItemDto> items,
            string term,
            int limit)
        {
            return items
                .OrderByDescending(i => i.Name.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();
        }

        /// <summary>
        /// Parses the optional integer parent identifier. An empty value means a
        /// global search; an unparsable value is reported as invalid.
        /// </summary>
        private static bool TryGetIntParent(string? parentId, out int? value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(parentId))
                return true;

            if (int.TryParse(parentId.Trim(), out var parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses the optional Guid parent identifier. An empty value means a
        /// global search; an unparsable value is reported as invalid.
        /// </summary>
        private static bool TryGetGuidParent(string? parentId, out Guid? value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(parentId))
                return true;

            if (Guid.TryParse(parentId.Trim(), out var parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }
    }
}
