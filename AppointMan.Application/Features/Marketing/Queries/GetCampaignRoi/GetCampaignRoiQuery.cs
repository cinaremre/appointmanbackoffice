using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Marketing.Queries.GetCampaignRoi
{
    /// <summary>
    /// Kampanya donem ozeti: toplam harcama, atfedilen randevu sayisi,
    /// atfedilen gerceklesen randevu sayisi. Ilk surumde klasik ROAS
    /// hesaplanmaz; detay DTO icindeki RoasDesc alaninda aciklanir.
    /// </summary>
    public class GetCampaignRoiQuery : IRequest<Result<CampaignRoiDto>>
    {
        public Guid CampaignId { get; set; }

        /// <summary>Opsiyonel donem filtresi (harcama tarihi ve atif zamani).</summary>
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
