using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinesses
{
    /// <summary>
    /// Backoffice işletme listesi (doküman Modül 8). Status ad, arama terimi
    /// ve şehir ile filtrelenebilir; sayfalama AdminPagination kurallarına göre
    /// düzenlenir.
    /// </summary>
    public class GetBusinessesQuery : IRequest<Result<AdminBusinessListResult>>
    {
        /// <summary>"Active" veya "Suspended"; boş ise tüm işletmeler.</summary>
        public string? Status { get; set; }

        /// <summary>İşletme adı içerik araması (normalize edilir).</summary>
        public string? Search { get; set; }

        /// <summary>Adres üzerinden şehir filtresi.</summary>
        public int? CityId { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 50;
    }

    /// <summary>Sayfalanmış işletme listesi sonucu.</summary>
    public class AdminBusinessListResult
    {
        public IReadOnlyList<AdminBusinessListItemDto> Items { get; init; } = Array.Empty<AdminBusinessListItemDto>();

        public int Page { get; init; }

        public int PageSize { get; init; }

        public long TotalCount { get; init; }
    }

    /// <summary>İşletme liste satırı (doküman Modül 8.2 alanları).</summary>
    public class AdminBusinessListItemDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        /// <summary>"Active" veya "Suspended".</summary>
        public string Status { get; init; } = string.Empty;

        public int CityId { get; init; }

        public Guid OwnerId { get; init; }

        public DateTime CreatedAt { get; init; }

        public decimal? Rating { get; init; }

        public int TotalReviews { get; init; }
    }
}
