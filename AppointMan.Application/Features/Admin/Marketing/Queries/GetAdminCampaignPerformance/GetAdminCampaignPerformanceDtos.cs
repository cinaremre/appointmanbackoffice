namespace AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminCampaignPerformance
{
    /// <summary>
    /// Kampanya performans ozeti. Atfedilen randevular henuz satis tutari
    /// tasimadigi icin klasik ROAS hesabi yapilmaz; aciklama RoasDesc alanindadir.
    /// </summary>
    public class AdminCampaignPerformanceDto
    {
        public Guid CampaignId { get; set; }
        public Guid BusinessId { get; set; }
        public string CampaignName { get; set; } = string.Empty;

        /// <summary>Kampanyaya yapilan toplam harcama.</summary>
        public decimal TotalSpend { get; set; }

        /// <summary>Kampanyaya atfedilen randevu sayisi.</summary>
        public int AttributedAppointments { get; set; }

        /// <summary>Kampanyaya atfedilen ve durumu Completed olan randevu sayisi.</summary>
        public int AttributedCompleted { get; set; }

        /// <summary>ROAS aciklamasi; GetCampaignRoi deseniyle uyumludur.</summary>
        public string RoasDesc { get; set; } = string.Empty;
    }
}
