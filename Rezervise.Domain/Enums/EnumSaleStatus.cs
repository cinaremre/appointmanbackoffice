namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Satış durumları. Payment ve Expense kayıtlarının durumu da
    /// aynı değer kümesini paylaşır; aykırı bir ödeme/gider durumu
    /// gerekmediği sürece yeni enüm üretilmez.
    /// </summary>
    public enum EnumSaleStatus
    {
        /// <summary>
        /// Beklemede - Satış oluşturuldu, henüz tamamlanmadı
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Tamamlandı - Satış kapanmışs, ödemeleri alındı
        /// </summary>
        Completed = 1,

        /// <summary>
        /// İptal edildi - Satış iptal edildi
        /// </summary>
        Cancelled = 2,

        /// <summary>
        /// İade edildi - Satışın tamamı iade edildi
        /// </summary>
        Refunded = 3
    }
}
