using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminCampaigns
{
    /// <summary>
    /// Platform kampanya listesi (doküman Modül 15). Her kampanya için toplam
    /// harcama ve atfedilen randevu sayısı yanında taşır.
    /// </summary>
    public class GetAdminCampaignsQuery : IRequest<Result<AdminCampaignListDto>>
    {
        /// <summary>Yalnızca bu işletmenin kampanyaları.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Kampanya kanalı filtresi.</summary>
        public EnumCampaignChannel? Channel { get; set; }

        /// <summary>Aktiflik filtresi (true: aktif, false: pasif).</summary>
        public bool? IsActive { get; set; }

        /// <summary>1 tabanlı sayfa numarası; kırpanma kuralı AdminPagination'dadır.</summary>
        public int Page { get; set; }

        /// <summary>Sayfa boyutu; en fazla 200, varsayılan 50.</summary>
        public int PageSize { get; set; }
    }
}
