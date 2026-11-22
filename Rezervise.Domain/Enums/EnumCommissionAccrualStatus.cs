namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Prim tahakkuk durumlari. Tahakkuklar yalnizca ileri yonde
    /// Accrued -> Paid gecisine sahiptir; Accrued -> Reversed geri alma
    /// Reverse() metodu ile, Paid durumundan geri alma kabul edilmez.
    /// </summary>
    public enum EnumCommissionAccrualStatus
    {
        /// <summary>Tahakkuk olustu, odeme bekliyor.</summary>
        Accrued = 0,

        /// <summary>Odeme doneminde odenmis.</summary>
        Paid = 1,

        /// <summary>Tahakkuk ters cevrilmis (iptal/iade).</summary>
        Reversed = 2
    }
}
