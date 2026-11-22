using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Services.Queries.GetMyServices
{
    /// <summary>
    /// Isletme sahibinin kendi hizmet listesi. Sahiplik handler'da
    /// dogrulanir; ad aramasi ve durum filtresi desteklenir.
    /// </summary>
    public class GetMyServicesQuery : IRequest<Result<AdminPagedResult<OwnerServiceListItemDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Hizmet adi icerik aramasi.</summary>
        public string? Search { get; set; }

        /// <summary>Aktif/pasif hizmet filtresi.</summary>
        public bool? IsActive { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner hizmet liste satiri.</summary>
    public class OwnerServiceListItemDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        /// <summary>Fiyat tutari; fiyat tanimli degilse null.</summary>
        public decimal? Price { get; init; }

        public string? Currency { get; init; }

        public int DurationMinutes { get; init; }

        public bool IsActive { get; init; }
    }
}
