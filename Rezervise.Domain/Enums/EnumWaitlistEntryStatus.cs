namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Bekleme listesi kaydının durumu (doküman Modül 10).
    /// </summary>
    public enum EnumWaitlistEntryStatus
    {
        /// <summary>Kayıt açık; uygun slot çıktığında teklif üretilebilir.</summary>
        Active = 0,

        /// <summary>Müşteriye gönderilen teklifden biri kabul edildi.</summary>
        Fulfilled = 1,

        /// <summary>Müşteri kaydını kendisi veya işletme iptal etti.</summary>
        Cancelled = 2,

        /// <summary>Kaydın geçerlilik süresi doldu.</summary>
        Expired = 3
    }
}
