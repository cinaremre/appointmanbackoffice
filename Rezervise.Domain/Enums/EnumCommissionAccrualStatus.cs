namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Prim tahakkuk durumları. Tahakkuklar yalnızca ileri yönde
    /// Accrued -> Paid geçişine sahiptir; Accrued -> Reversed geri alma
    /// Reverse() metodu ile, Paid durumundan geri alma kabul edilmez.
    /// </summary>
    public enum EnumCommissionAccrualStatus
    {
        /// <summary>Tahakkuk oluştu, ödeme bekliyor.</summary>
        Accrued = 0,

        /// <summary>Ödeme döneminde ödenmiş.</summary>
        Paid = 1,

        /// <summary>Tahakkuk ters çevrilmiş (iptal/iade).</summary>
        Reversed = 2
    }
}
