namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Kullanici rolleri
    /// </summary>
    public enum EnumUserRole
    {
        /// <summary>
        /// Müsteri - Randevu alan kullanici
        /// </summary>
        Customer = 0,

        /// <summary>
        /// Isletme Sahibi - Isletme yöneten kullanici
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
        /// Çalisan - Isletmede çalisan personel
        /// </summary>
        Employee = 4,

        /// <summary>
        /// Moderatör - Içerik ve kullanici moderasyonu
        /// </summary>
        Moderator = 5,

        /// <summary>
        /// Destek - Müsteri destek personeli
        /// </summary>
        Support = 6,

        /// <summary>
        /// Platform Yoneticisi - Backoffice'te isletme/kategori/operasyon yonetimi
        /// </summary>
        PlatformAdmin = 7,

        /// <summary>
        /// Finans Yoneticisi - Backoffice'te abonelik, odeme ve finansal raporlar
        /// </summary>
        FinanceAdmin = 8,

        /// <summary>
        /// Destek Temsilcisi - Backoffice'te sinirli operasyonel mudahale
        /// </summary>
        SupportAgent = 9,

        /// <summary>
        /// Pazarlama Yoneticisi - Backoffice'te kampanya ve promosyon yonetimi
        /// </summary>
        MarketingAdmin = 10,

        /// <summary>
        /// Icerik Yoneticisi - Backoffice'te kategori ve hizmet siniflandirmasi
        /// </summary>
        ContentManager = 11,

        /// <summary>
        /// Denetci - Backoffice'te salt okunur audit ve operasyon gecmisi
        /// </summary>
        Auditor = 12,

        /// <summary>
        /// Salt Okunur Analist - Yetkilendirilen raporlari goruntuleme
        /// </summary>
        ReadOnlyAnalyst = 13
    }
}