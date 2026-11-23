namespace Rezervise.Application.Features.Reporting.Queries.GetBusinessDashboard
{
    /// <summary>
    /// Yönetici dashboard günlük kartları (doküman Modül 15.1).
    /// </summary>
    public class BusinessDashboardDto
    {
        public Guid BusinessId { get; set; }

        /// <summary>Kartların hesaplandığı gün (UTC).</summary>
        public DateTime Date { get; set; }

        /// <summary>Bugünkü beklemede/onaylı randevu sayısı.</summary>
        public int BugunkiRandevular { get; set; }

        /// <summary>Bugün tamamlanan randevu sayısı.</summary>
        public int Tamamlanan { get; set; }

        /// <summary>Bugün iptal edilen randevu sayısı.</summary>
        public int Iptaller { get; set; }

        /// <summary>Bugün gelmeyen (no-show) randevu sayısı.</summary>
        public int NoShow { get; set; }

        /// <summary>
        /// Basit doluluk oranı: (tamamlanan + aktif randevu toplam süresi) /
        /// (aktif çalışan sayısı * günün işletme açık saat toplamı). 0-1 arası,
        /// iki hanede yuvarlanır. Kaynak çakışmaları sonraki sürümde ele alınacaktır.
        /// </summary>
        public decimal DolulukOrani { get; set; }
    }
}
