using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Services.Queries.GetMyServices
{
    /// <summary>
    /// İşletme sahibinin kendi hizmet listesi. Sahiplik handler'da
    /// doğrulanır; ad araması ve durum filtresi desteklenir.
    /// </summary>
    public class GetMyServicesQuery : IRequest<Result<AdminPagedResult<OwnerServiceListItemDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Hizmet adı içerik araması.</summary>
        public string? Search { get; set; }

        /// <summary>Aktif/pasif hizmet filtresi.</summary>
        public bool? IsActive { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sınırlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner hizmet liste satırı.</summary>
    public class OwnerServiceListItemDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        /// <summary>Fiyat tutarı; fiyat tanımlı değilse null.</summary>
        public decimal? Price { get; init; }

        public string? Currency { get; init; }

        public int DurationMinutes { get; init; }

        public bool IsActive { get; init; }
    }
}
