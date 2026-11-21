namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Bekleme listesi kaydinin durumu (dokuman Modul 10).
    /// </summary>
    public enum EnumWaitlistEntryStatus
    {
        /// <summary>Kayit acik; uygun slot ciktiginda teklif uretilebilir.</summary>
        Active = 0,

        /// <summary>Musteriye gonderilen teklifden biri kabul edildi.</summary>
        Fulfilled = 1,

        /// <summary>Musteri kaydini kendisi veya isletme iptal etti.</summary>
        Cancelled = 2,

        /// <summary>Kaydin gecerlilik suresi doldu.</summary>
        Expired = 3
    }
}
