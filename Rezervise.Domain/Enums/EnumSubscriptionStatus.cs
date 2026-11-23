namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// İşletme aboneliğinin durumu (doküman Modül 14).
    /// </summary>
    public enum EnumSubscriptionStatus
    {
        /// <summary>Abonelik aktif; plan özellikleri kullanılabilir.</summary>
        Active = 0,

        /// <summary>Süre doldu veya yeni abonelikle değiştirildi.</summary>
        Expired = 1,

        /// <summary>İşletme aboneliği iptal etti.</summary>
        Cancelled = 2
    }
}
