using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Businesses.Queries.GetMyBusinesses
{
    /// <summary>
    /// İşletme sahibinin kendi işletme listesi. Sahiplik JWT kimliğinden
    /// çözülür; istekte işletme/sahip filtresi yoktur, yanıt her zaman
    /// çağrıya ait kayıtlarla sınırlıdır.
    /// </summary>
    public class GetMyBusinessesQuery : IRequest<Result<AdminPagedResult<OwnerBusinessListItemDto>>>
    {
        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sınırlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>İşletme sahibinin işletme liste satırı.</summary>
    public class OwnerBusinessListItemDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public decimal? Rating { get; init; }

        public int TotalReviews { get; init; }

        public int CityId { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}
