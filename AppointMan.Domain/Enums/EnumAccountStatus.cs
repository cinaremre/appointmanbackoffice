namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Kullanıcı hesap durumu
    /// </summary>
    public enum EnumAccountStatus
    {
        /// <summary>
        /// Aktif - Hesap normal şekilde kullanılıyor
        /// </summary>
        Active = 0,

        /// <summary>
        /// Pasif - Hesap geçici olarak devre dışı
        /// </summary>
        Inactive = 1,

        /// <summary>
        /// Kilitli - Hesap güvenlik nedeniyle kilitlendi
        /// </summary>
        Locked = 2,

        /// <summary>
        /// Doğrulama Bekliyor - Hesap onay bekliyor
        /// </summary>
        PendingVerification = 3,

        /// <summary>
        /// Silinmiş - Hesap kalıcı olarak silindi
        /// </summary>
        Deleted = 4
    }
}