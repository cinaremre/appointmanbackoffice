namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Prim odeme (payout) donem durumlari.
    /// </summary>
    public enum EnumCommissionPayoutStatus
    {
        /// <summary>Odeme bekliyor.</summary>
        Pending = 0,

        /// <summary>Odenmis.</summary>
        Paid = 1
    }
}
