namespace AppointMan.Application.Features.Reporting.Queries.GetBusinessDashboard
{
    /// <summary>
    /// Yonetici dashboard gunluk kartlari (dokuman Modul 15.1).
    /// </summary>
    public class BusinessDashboardDto
    {
        public Guid BusinessId { get; set; }

        /// <summary>Kartlarin hesaplandigi gun (UTC).</summary>
        public DateTime Date { get; set; }

        /// <summary>Bugunku beklemede/onayli randevu sayisi.</summary>
        public int BugunkiRandevular { get; set; }

        /// <summary>Bugun tamamlanan randevu sayisi.</summary>
        public int Tamamlanan { get; set; }

        /// <summary>Bugun iptal edilen randevu sayisi.</summary>
        public int Iptaller { get; set; }

        /// <summary>Bugun gelmeyen (no-show) randevu sayisi.</summary>
        public int NoShow { get; set; }

        /// <summary>
        /// Basit doluluk orani: (tamamlanan + aktif randevu toplam suresi) /
        /// (aktif calisan sayisi * gunun isletme acik saat toplami). 0-1 arasi,
        /// iki hanede yuvarlanir. Kaynak cakismalari sonraki surumde ele alinacaktir.
        /// </summary>
        public decimal DolulukOrani { get; set; }
    }
}
