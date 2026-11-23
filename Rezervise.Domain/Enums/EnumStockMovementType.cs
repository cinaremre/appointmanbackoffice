namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Stok hareket tipleri. Giriş tipleri bakiyeyi artırır,
    /// çıkış tipleri azaltır (işaret StockMovement constructor'ında belirlenir).
    /// </summary>
    public enum EnumStockMovementType
    {
        /// <summary>Tedarikçiden satın alma girişi (+)</summary>
        PurchaseIn = 0,

        /// <summary>Satış çıkışı (-)</summary>
        SaleOut = 1,

        /// <summary>Hizmet tüketimi, randevu malzeme sarfi (-)</summary>
        ServiceConsumption = 2,

        /// <summary>Zayı/atik (-)</summary>
        Waste = 3,

        /// <summary>Müşteri iadesi (+)</summary>
        CustomerReturn = 4,

        /// <summary>Sayım farkı düzeltmesi (işaret miktara göre)</summary>
        StockCountAdjustment = 5,

        /// <summary>Tedarikçiye iade (-)</summary>
        SupplierReturn = 6
    }
}
