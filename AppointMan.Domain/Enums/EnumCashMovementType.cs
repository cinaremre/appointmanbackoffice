namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Kasa hareket tipleri
    /// </summary>
    public enum EnumCashMovementType
    {
        /// <summary>Kasaya nakit girisi (navlon, avans iadesi vb.)</summary>
        CashIn = 0,

        /// <summary>Kasadan nakit cikisi (navlon, iade vb.)</summary>
        CashOut = 1,

        /// <summary>Kasa bastan sayim duzeltmesi (fiziksel nakit transferi)</summary>
        CashDrop = 2,

        /// <summary>Yeniden sayim duzeltmesi</summary>
        CashRecount = 3
    }
}
