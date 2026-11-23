namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminCampaignPerformance
{
    /// <summary>
    /// Kampanya performans özeti. Atfedilen randevular henüz satış tutarı
    /// taşımadığı için klasik ROAS hesabı yapılmaz; açıklama RoasDesc alanındadır.
    /// </summary>
    public class AdminCampaignPerformanceDto
    {
        public Guid CampaignId { get; set; }
        public Guid BusinessId { get; set; }
        public string CampaignName { get; set; } = string.Empty;

        /// <summary>Kampanyaya yapılan toplam harcama.</summary>
        public decimal TotalSpend { get; set; }

        /// <summary>Kampanyaya atfedilen randevu sayısı.</summary>
        public int AttributedAppointments { get; set; }

        /// <summary>Kampanyaya atfedilen ve durumu Completed olan randevu sayısı.</summary>
        public int AttributedCompleted { get; set; }

        /// <summary>ROAS açıklaması; GetCampaignRoi deseniyle uyumludur.</summary>
        public string RoasDesc { get; set; } = string.Empty;
    }
}
