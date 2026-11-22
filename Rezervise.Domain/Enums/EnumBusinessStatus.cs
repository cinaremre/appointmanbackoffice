namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Isletme durumu
    /// </summary>
    public enum EnumBusinessStatus
    {
        /// <summary>
        /// Aktif - Isletme aktif ve hizmet veriyor
        /// </summary>
        Active = 0,

        /// <summary>
        /// Pasif - Isletme geçici olarak kapali
        /// </summary>
        Inactive = 1,

        /// <summary>
        /// Dogrulama Bekliyor - Isletme onay bekliyor
        /// </summary>
        PendingVerification = 2,

        /// <summary>
        /// Askida - Isletme geçici olarak askiya alindi
        /// </summary>
        Suspended = 3,

        /// <summary>
        /// Kapali - Isletme kalici olarak kapandi
        /// </summary>
        Closed = 4
    }
}