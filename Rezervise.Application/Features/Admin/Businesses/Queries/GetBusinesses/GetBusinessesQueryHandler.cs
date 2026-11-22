using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinesses
{
    /// <summary>
    /// Isletme listesi handler'i (dokuman Modul 8). Ilk surum filtrelemeyi
    /// repository uzerinde bellek icinde yapar; buyuk olcekte ozet/read model
    /// tablosuna tasinacaktir.
    /// </summary>
    public class GetBusinessesQueryHandler : IRequestHandler<GetBusinessesQuery, Result<AdminBusinessListResult>>
    {
        private readonly IRepository<Business> _businessRepository;

        public GetBusinessesQueryHandler(IRepository<Business> businessRepository)
        {
            _businessRepository = businessRepository;
        }

        public async Task<Result<AdminBusinessListResult>> Handle(GetBusinessesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sayfalama kurallari: page alt siniri 1, pageSize 200 ust siniri.
                var page = AdminPagination.NormalizePage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                // 2. Durum filtresi: entity IsActive bayragi uzerinden
                //    "Active"/"Suspended" olarak ifade edilir.
                Expression<Func<Business, bool>> predicate = b => !b.IsDeleted;

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    var status = request.Status.Trim();

                    if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        predicate = predicate.AndAlso(b => b.IsActive);
                    }
                    else if (string.Equals(status, "Suspended", StringComparison.OrdinalIgnoreCase))
                    {
                        predicate = predicate.AndAlso(b => !b.IsActive);
                    }
                    else
                    {
                        return Result<AdminBusinessListResult>.Failure(
                            Error.Validation("Status filtresi yalnizca Active veya Suspended olabilir."));
                    }
                }

                // 3. Ad aramasi: Turkce karakterleri dusuren normalizer ile icerik eslesmesi.
                var normalizedSearch = SearchNormalizer.Normalize(request.Search);

                if (!string.IsNullOrEmpty(normalizedSearch))
                {
                    predicate = predicate.AndAlso(
                        b => SearchNormalizer.Normalize(b.Name).Contains(normalizedSearch));
                }

                // 4. Sehir filtresi: adres value object uzerinden.
                if (request.CityId.HasValue)
                {
                    var cityId = request.CityId.Value;
                    predicate = predicate.AndAlso(b => b.Address.CityId == cityId);
                }

                // 5. Bellek ici filtre + sayfalama (ilk surum).
                var businesses = await _businessRepository.FindAsync(predicate, cancellationToken);

                var totalCount = businesses.Count;
                var items = businesses
                    .OrderBy(b => b.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new AdminBusinessListItemDto
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Status = b.IsActive ? "Active" : "Suspended",
                        CityId = b.Address?.CityId ?? 0,
                        OwnerId = b.OwnerId,
                        CreatedAt = b.CreatedAt,
                        Rating = b.Rating,
                        TotalReviews = b.TotalReviews
                    })
                    .ToList();

                return Result<AdminBusinessListResult>.Success(new AdminBusinessListResult
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return Result<AdminBusinessListResult>.Failure(
                    Error.Internal("Isletme listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
