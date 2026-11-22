namespace Rezervise.Application.Common.DTOs
{
    /// <summary>
    /// Kampanya ROAS/ROI ozeti. Ilk surumde atfedilen randevular henuz
    /// satis tutari tasimadigi icin klasik ROAS hesaplanmaz.
    /// </summary>
    public class CampaignRoiDto
    {
        public Guid CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;

        /// <summary>Donemdeki toplam harcama.</summary>
        public decimal TotalSpend { get; set; }

        /// <summary>Kampanyaya atfedilen randevu sayisi.</summary>
        public int AttributedAppointments { get; set; }

        /// <summary>Kampanyaya atfedilen ve gerceklesen randevu sayisi.</summary>
        public int AttributedCompleted { get; set; }

        /// <summary>
        /// ROAS aciklama metni: satis entegrasyonu sonrasi gercek ROAS hesaplanacak.
        /// </summary>
        public string RoasDesc { get; set; } = string.Empty;
    }
}
