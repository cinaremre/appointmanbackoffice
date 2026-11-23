namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Kasa hareket tipleri
    /// </summary>
    public enum EnumCashMovementType
    {
        /// <summary>Kasaya nakit girişi (navlon, avans iadesi vb.)</summary>
        CashIn = 0,

        /// <summary>Kasadan nakit çıkışı (navlon, iade vb.)</summary>
        CashOut = 1,

        /// <summary>Kasa baştan sayım düzeltmesi (fiziksel nakit transferi)</summary>
        CashDrop = 2,

        /// <summary>Yeniden sayım düzeltmesi</summary>
        CashRecount = 3
    }
}
