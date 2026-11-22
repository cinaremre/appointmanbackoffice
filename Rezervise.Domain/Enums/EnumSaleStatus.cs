namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Satis durumlari. Payment ve Expense kayitlarinin durumu da
    /// ayni deger kumesini paylasir; aykiri bir odeme/gider durumu
    /// gerekmedigi surece yeni enum uretilmez.
    /// </summary>
    public enum EnumSaleStatus
    {
        /// <summary>
        /// Beklemede - Satis olusturuldu, henuz tamamlanmadi
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Tamamlandi - Satis kapanmiss, odemeleri alindi
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Iptal edildi - Satis iptal edildi
        /// </summary>
        Cancelled = 2,

        /// <summary>
        /// Iade edildi - Satisin tamami iade edildi
        /// </summary>
        Refunded = 3
    }
}
