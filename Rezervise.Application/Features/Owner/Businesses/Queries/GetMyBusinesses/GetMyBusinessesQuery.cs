using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Businesses.Queries.GetMyBusinesses
{
    /// <summary>
    /// Isletme sahibinin kendi isletme listesi. Sahiplik JWT kimliginden
    /// cozulur; istekte isletme/sahip filtresi yoktur, yanit her zaman
    /// cagriya ait kayitlarla sinirlidir.
    /// </summary>
    public class GetMyBusinessesQuery : IRequest<Result<AdminPagedResult<OwnerBusinessListItemDto>>>
    {
        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Isletme sahibinin isletme liste satiri.</summary>
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
