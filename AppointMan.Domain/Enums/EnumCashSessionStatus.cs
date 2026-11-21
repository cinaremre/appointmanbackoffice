namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Kasa oturumu durumlari
    /// </summary>
    public enum EnumCashSessionStatus
    {
        /// <summary>Acik - Oturum devam ediyor</summary>
        Open = 0,

        /// <summary>Kapali - Oturum kapatildi, fark rapolandi</summary>
        Closed = 1
    }
}
