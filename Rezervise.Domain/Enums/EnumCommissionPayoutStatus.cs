namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Prim ödeme (payout) dönem durumları.
    /// </summary>
    public enum EnumCommissionPayoutStatus
    {
        /// <summary>Ödeme bekliyor.</summary>
        Pending = 0,

        /// <summary>Ödenmiş.</summary>
        Paid = 1
    }
}
