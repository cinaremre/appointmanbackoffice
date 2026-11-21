namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Komisyon (prim) hesaplama tipleri. Yuzde tiplerinde
    /// CommissionRule.Rate alani 0-1 arasi oran tasir (orn. %20 = 0.20).
    /// </summary>
    public enum EnumCommissionCalculationType
    {
        /// <summary>Hizmet bedelinin yuzdesi; baz: hizmet tutari.</summary>
        ServicePercent = 0,

        /// <summary>Calisanin urettigi toplam cirosunun yuzdesi.</summary>
        EmployeePercent = 1,

        /// <summary>Hizmet basina sabit tutar (FixedAmount).</summary>
        FixedPerService = 2,

        /// <summary>Urun satis bedelinin yuzdesi.</summary>
        ProductPercent = 3,

        /// <summary>Kademeli yuzde (ilk surunde Rate ile ayni sekilde uygulanir).</summary>
        TieredPercent = 4
    }
}
