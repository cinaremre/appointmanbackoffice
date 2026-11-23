using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Marketing.Queries.GetCampaignRoi
{
    /// <summary>
    /// Kampanya dönem özeti: toplam harcama, atfedilen randevu sayısı,
    /// atfedilen gerçekleşen randevu sayısı. İlk sürümde klasik ROAS
    /// hesaplanmaz; detay DTO içindeki RoasDesc alanında açıklanır.
    /// </summary>
    public class GetCampaignRoiQuery : IRequest<Result<CampaignRoiDto>>
    {
        public Guid CampaignId { get; set; }

        /// <summary>Opsiyonel dönem filtresi (harcama tarihi ve atıf zamanı).</summary>
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
