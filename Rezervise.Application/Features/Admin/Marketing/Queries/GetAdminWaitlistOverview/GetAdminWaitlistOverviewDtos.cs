namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminWaitlistOverview
{
    /// <summary>
    /// Bekleme listesi özet sayıları. Teklif kırılımı Pending/Accepted/Expired;
    /// kurtarılan slot kırılımı Rebooked/Lost olarak raporlanır.
    /// </summary>
    public class AdminWaitlistOverviewDto
    {
        /// <summary>Raporin işletme filtresi; platform geneli raporda boş.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Aktif (Open) bekleme listesi kayıt sayısı.</summary>
        public int ActiveEntries { get; set; }

        /// <summary>Yanıt bekleyen teklif sayısı.</summary>
        public int PendingOffers { get; set; }

        /// <summary>Kabul edilen teklif sayısı.</summary>
        public int AcceptedOffers { get; set; }

        /// <summary>Süresi dolan teklif sayısı.</summary>
        public int ExpiredOffers { get; set; }

        /// <summary>Bekleme listesinden yeniden doldurulan slot sayısı.</summary>
        public int RebookedSlots { get; set; }

        /// <summary>Doldurulamadan kaybedilen slot sayısı.</summary>
        public int LostSlots { get; set; }
    }
}
