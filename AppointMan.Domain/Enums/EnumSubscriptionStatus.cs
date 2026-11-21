namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Isletme aboneliginin durumu (dokuman Modul 14).
    /// </summary>
    public enum EnumSubscriptionStatus
    {
        /// <summary>Abonelik aktif; plan ozellikleri kullanilabilir.</summary>
        Active = 0,

        /// <summary>Sure doldu veya yeni abonelikle degistirildi.</summary>
        Expired = 1,

        /// <summary>Isletme aboneligi iptal etti.</summary>
        Cancelled = 2
    }
}
