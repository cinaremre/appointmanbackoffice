namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Bekleme listesi slot teklifinin durumu (dokuman Modul 10).
    /// </summary>
    public enum EnumWaitlistOfferStatus
    {
        /// <summary>Teklif gonderildi, yanit bekleniyor.</summary>
        Pending = 0,

        /// <summary>Musteri teklifi kabul etti.</summary>
        Accepted = 1,

        /// <summary>Musteri teklifi reddetti.</summary>
        Declined = 2,

        /// <summary>Yanit suresi doldu.</summary>
        Expired = 3,

        /// <summary>Ayni slot icin baska bir teklif kabul edildigi icin gecersizlesti.</summary>
        Superseded = 4
    }
}
