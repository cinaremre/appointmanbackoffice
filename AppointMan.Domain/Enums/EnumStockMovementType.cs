namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Stok hareket tipleri. Giris tipleri bakiyeyi artirir,
    /// cikis tipleri azaltir (isaret StockMovement constructor'inda belirlenir).
    /// </summary>
    public enum EnumStockMovementType
    {
        /// <summary>Tedarikciden satin alma girisi (+)</summary>
        PurchaseIn = 0,

        /// <summary>Satis cikisi (-)</summary>
        SaleOut = 1,

        /// <summary>Hizmet tuketimi, randevu malzeme sarfi (-)</summary>
        ServiceConsumption = 2,

        /// <summary>Zayi/atik (-)</summary>
        Waste = 3,

        /// <summary>Musteri iadesi (+)</summary>
        CustomerReturn = 4,

        /// <summary>Sayim farki duzeltmesi (isaret miktara gore)</summary>
        StockCountAdjustment = 5,

        /// <summary>Tedarikciye iade (-)</summary>
        SupplierReturn = 6
    }
}
