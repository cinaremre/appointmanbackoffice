using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinesses
{
    /// <summary>
    /// Backoffice isletme listesi (dokuman Modul 8). Status ad, arama terimi
    /// ve sehir ile filtrelenebilir; sayfalama AdminPagination kurallarina gore
    /// duzenlenir.
    /// </summary>
    public class GetBusinessesQuery : IRequest<Result<AdminBusinessListResult>>
    {
        /// <summary>"Active" veya "Suspended"; bos ise tum isletmeler.</summary>
        public string? Status { get; set; }

        /// <summary>Isletme adi icerik aramasi (normalize edilir).</summary>
        public string? Search { get; set; }

        /// <summary>Adres uzerinden sehir filtresi.</summary>
        public int? CityId { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 50;
    }

    /// <summary>Sayfalanmis isletme listesi sonucu.</summary>
    public class AdminBusinessListResult
    {
        public IReadOnlyList<AdminBusinessListItemDto> Items { get; init; } = Array.Empty<AdminBusinessListItemDto>();

        public int Page { get; init; }

        public int PageSize { get; init; }

        public long TotalCount { get; init; }
    }

    /// <summary>Isletme liste satiri (dokuman Modul 8.2 alanlari).</summary>
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
