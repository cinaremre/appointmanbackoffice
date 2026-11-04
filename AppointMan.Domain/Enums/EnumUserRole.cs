namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Kullanıcı rolleri
    /// </summary>
    public enum EnumUserRole
    {
        /// <summary>
        /// Müşteri - Randevu alan kullanıcı
        /// </summary>
        Customer = 0,

        /// <summary>
        /// İşletme Sahibi - İşletme yöneten kullanıcı
        /// </summary>
        BusinessOwner = 1,

        /// <summary>
        /// Admin - Sistem yöneticisi
        /// </summary>
        Admin = 2,

        /// <summary>
        /// Süper Admin - Tüm yetkilere sahip sistem yöneticisi
        /// </summary>
        SuperAdmin = 3,

        /// <summary>
        /// Çalışan - İşletmede çalışan personel
        /// </summary>
        Employee = 4,

        /// <summary>
        /// Moderatör - İçerik ve kullanıcı moderasyonu
        /// </summary>
        Moderator = 5,

        /// <summary>
        /// Destek - Müşteri destek personeli
        /// </summary>
        Support = 6
    }
}