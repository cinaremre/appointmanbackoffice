namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Kasa oturumu durumları
    /// </summary>
    public enum EnumCashSessionStatus
    {
        /// <summary>Açık - Oturum devam ediyor</summary>
        Open = 0,

        /// <summary>Kapalı - Oturum kapatıldı, fark rapolandı</summary>
        Closed = 1
    }
}
