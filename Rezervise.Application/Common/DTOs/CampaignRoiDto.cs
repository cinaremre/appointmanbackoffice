namespace Rezervise.Application.Common.DTOs
{
    /// <summary>
    /// Kampanya ROAS/ROI özeti. İlk sürümde atfedilen randevular henüz
    /// satış tutarı taşımadığı için klasik ROAS hesaplanmaz.
    /// </summary>
    public class CampaignRoiDto
    {
        public Guid CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;

        /// <summary>Dönemdeki toplam harcama.</summary>
        public decimal TotalSpend { get; set; }

        /// <summary>Kampanyaya atfedilen randevu sayısı.</summary>
        public int AttributedAppointments { get; set; }

        /// <summary>Kampanyaya atfedilen ve gerçekleşen randevu sayısı.</summary>
        public int AttributedCompleted { get; set; }

        /// <summary>
        /// ROAS açıklama metni: satış entegrasyonu sonrası gerçek ROAS hesaplanacak.
        /// </summary>
        public string RoasDesc { get; set; } = string.Empty;
    }
}
