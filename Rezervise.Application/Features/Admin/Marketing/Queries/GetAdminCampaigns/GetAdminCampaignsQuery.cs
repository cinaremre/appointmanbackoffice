using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminCampaigns
{
    /// <summary>
    /// Platform kampanya listesi (dokuman Modul 15). Her kampanya icin toplam
    /// harcama ve atfedilen randevu sayisi yaninda tasir.
    /// </summary>
    public class GetAdminCampaignsQuery : IRequest<Result<AdminCampaignListDto>>
    {
        /// <summary>Yalnizca bu isletmenin kampanyalari.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Kampanya kanali filtresi.</summary>
        public EnumCampaignChannel? Channel { get; set; }

        /// <summary>Aktiflik filtresi (true: aktif, false: pasif).</summary>
        public bool? IsActive { get; set; }

        /// <summary>1 tabanli sayfa numarasi; kirpanma kurali AdminPagination'dadir.</summary>
        public int Page { get; set; }

        /// <summary>Sayfa boyutu; en fazla 200, varsayilan 50.</summary>
        public int PageSize { get; set; }
    }
}
