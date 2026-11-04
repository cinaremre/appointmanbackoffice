namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// İşletme durumu
    /// </summary>
    public enum EnumBusinessStatus
    {
        /// <summary>
        /// Aktif - İşletme aktif ve hizmet veriyor
        /// </summary>
        Active = 0,

        /// <summary>
        /// Pasif - İşletme geçici olarak kapalı
        /// </summary>
        Inactive = 1,

        /// <summary>
        /// Doğrulama Bekliyor - İşletme onay bekliyor
        /// </summary>
        PendingVerification = 2,

        /// <summary>
        /// Askıda - İşletme geçici olarak askıya alındı
        /// </summary>
        Suspended = 3,

        /// <summary>
        /// Kapalı - İşletme kalıcı olarak kapandı
        /// </summary>
        Closed = 4
    }
}