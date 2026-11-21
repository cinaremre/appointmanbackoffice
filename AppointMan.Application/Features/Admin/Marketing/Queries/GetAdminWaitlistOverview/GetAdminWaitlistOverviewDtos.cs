namespace AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminWaitlistOverview
{
    /// <summary>
    /// Bekleme listesi ozet sayilari. Teklif kirilimi Pending/Accepted/Expired;
    /// kurtarilan slot kirilimi Rebooked/Lost olarak raporlanir.
    /// </summary>
    public class AdminWaitlistOverviewDto
    {
        /// <summary>Raporin isletme filtresi; platform geneli raporda bos.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Aktif (Open) bekleme listesi kayit sayisi.</summary>
        public int ActiveEntries { get; set; }

        /// <summary>Yanit bekleyen teklif sayisi.</summary>
        public int PendingOffers { get; set; }

        /// <summary>Kabul edilen teklif sayisi.</summary>
        public int AcceptedOffers { get; set; }

        /// <summary>Suresi dolan teklif sayisi.</summary>
        public int ExpiredOffers { get; set; }

        /// <summary>Bekleme listesinden yeniden doldurulan slot sayisi.</summary>
        public int RebookedSlots { get; set; }

        /// <summary>Doldurulamadan kaybedilen slot sayisi.</summary>
        public int LostSlots { get; set; }
    }
}
