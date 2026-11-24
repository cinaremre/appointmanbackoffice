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
    /// İşletme listesi handler'i (doküman Modül 8). İlk sürüm filtrelemeyi
    /// repository üzerinde bellek içinde yapar; büyük ölçekte özet/read model
    /// tablosuna taşınacaktır.
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
                // 1. Sayfalama kuralları: page alt sınırı 1, pageSize 200 üst sınırı.
                var page = AdminPagination.NormalizePage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                // 2. Durum filtresi: gösterilen rozetle birebir aynı mantık.
                //    Onay bekleyen işletme, aktif olsa bile "Active" sayılmaz.
                Expression<Func<Business, bool>> predicate = b => !b.IsDeleted;

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    var status = request.Status.Trim();

                    if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        predicate = predicate.AndAlso(b => b.IsApproved && b.IsActive);
                    }
                    else if (string.Equals(status, "Suspended", StringComparison.OrdinalIgnoreCase))
                    {
                        predicate = predicate.AndAlso(b => b.IsApproved && !b.IsActive);
                    }
                    else if (string.Equals(status, "PendingApproval", StringComparison.OrdinalIgnoreCase))
                    {
                        // Onay bekleyen işletmeler: mobil üzerinden kaydolmuş, backoffice
                        // incelemesi bekleyen kayıtlar.
                        predicate = predicate.AndAlso(b => !b.IsApproved);
                    }
                    else
                    {
                        return Result<AdminBusinessListResult>.Failure(
                            Error.Validation("Status filtresi yalnızca Active, Suspended veya PendingApproval olabilir."));
                    }
                }

                // 3. Ad araması: Türkçe karakterleri düşüren normalizer ile içerik eşleşmesi.
                var normalizedSearch = SearchNormalizer.Normalize(request.Search);

                if (!string.IsNullOrEmpty(normalizedSearch))
                {
                    predicate = predicate.AndAlso(
                        b => SearchNormalizer.Normalize(b.Name).Contains(normalizedSearch));
                }

                // 4. Şehir filtresi: adres value object üzerinden.
                if (request.CityId.HasValue)
                {
                    var cityId = request.CityId.Value;
                    predicate = predicate.AndAlso(b => b.Address.CityId == cityId);
                }

                // 5. Bellek içi filtre + sayfalama (ilk sürüm).
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
                        // Onay bekleyen durum diğerlerinin önündedir: onaylanmamış işletme
                        // aktif olsa bile halka açık olarak çalışmıyordur.
                        Status = !b.IsApproved
                            ? "PendingApproval"
                            : b.IsActive ? "Active" : "Suspended",
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
                    Error.Internal("İşletme listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
