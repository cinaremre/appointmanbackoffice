namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Komisyon (prim) hesaplama tipleri. Yüzde tiplerinde
    /// CommissionRule.Rate alani 0-1 arası oran taşır (orn. %20 = 0.20).
    /// </summary>
    public enum EnumCommissionCalculationType
    {
        /// <summary>Hizmet bedelinin yüzdesi; baz: hizmet tutarı.</summary>
        ServicePercent = 0,

        /// <summary>Çalışanın ürettiği toplam cirosunun yüzdesi.</summary>
        EmployeePercent = 1,

        /// <summary>Hizmet basına sabit tutar (FixedAmount).</summary>
        FixedPerService = 2,

        /// <summary>Ürün satış bedelinin yüzdesi.</summary>
        ProductPercent = 3,

        /// <summary>Kademeli yüzde (ilk süründe Rate ile aynı şekilde uygulanır).</summary>
        TieredPercent = 4
    }
}
