namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Kullanici hesap durumu
    /// </summary>
    public enum EnumAccountStatus
    {
        /// <summary>
        /// Aktif - Hesap normal sekilde kullaniliyor
        /// </summary>
        Active = 0,

        /// <summary>
        /// Pasif - Hesap geçici olarak devre disi
        /// </summary>
        Inactive = 1,

        /// <summary>
        /// Kilitli - Hesap güvenlik nedeniyle kilitlendi
        /// </summary>
        Locked = 2,

        /// <summary>
        /// Dogrulama Bekliyor - Hesap onay bekliyor
        /// </summary>
        PendingVerification = 3,

        /// <summary>
        /// Silinmis - Hesap kalici olarak silindi
        /// </summary>
        Deleted = 4
    }
}