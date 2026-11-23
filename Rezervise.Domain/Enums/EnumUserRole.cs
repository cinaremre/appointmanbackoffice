namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Kullanıcı rolleri
    /// </summary>
    public enum EnumUserRole
    {
        /// <summary>
        /// Müsteri - Randevu alan kullanıcı
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
        /// Çalisan - İşletmede çalisan personel
        /// </summary>
        Employee = 4,

        /// <summary>
        /// Moderatör - Içerik ve kullanıcı moderasyonu
        /// </summary>
        Moderator = 5,

        /// <summary>
        /// Destek - Müsteri destek personeli
        /// </summary>
        Support = 6,

        /// <summary>
        /// Platform Yöneticisi - Backoffice'te işletme/kategori/operasyon yönetimi
        /// </summary>
        PlatformAdmin = 7,

        /// <summary>
        /// Finans Yöneticisi - Backoffice'te abonelik, ödeme ve finansal raporlar
        /// </summary>
        FinanceAdmin = 8,

        /// <summary>
        /// Destek Temsilcisi - Backoffice'te sınırlı operasyonel müdahale
        /// </summary>
        SupportAgent = 9,

        /// <summary>
        /// Pazarlama Yöneticisi - Backoffice'te kampanya ve promosyon yönetimi
        /// </summary>
        MarketingAdmin = 10,

        /// <summary>
        /// İçerik Yöneticisi - Backoffice'te kategori ve hizmet sınıflandırması
        /// </summary>
        ContentManager = 11,

        /// <summary>
        /// Denetçi - Backoffice'te salt okunur audit ve operasyon geçmişi
        /// </summary>
        Auditor = 12,

        /// <summary>
        /// Salt Okunur Analist - Yetkilendirilen raporları görüntüleme
        /// </summary>
        ReadOnlyAnalyst = 13
    }
}