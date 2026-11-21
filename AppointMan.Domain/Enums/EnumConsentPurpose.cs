namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Iletisim izninin kullanim amaci (dokuman Modul 16).
    /// Amac iznin kapsamini belirler: pazarlama izni baska bir amac icin
    /// kullanilamaz.
    /// </summary>
    public enum EnumConsentPurpose
    {
        /// <summary>Randevu bildirimi, hatirlatma gibi islemsel mesajlar.</summary>
        Transactional = 0,

        /// <summary>Kampanya, geri cagirma gibi pazarlama mesajlari.</summary>
        Marketing = 1,

        /// <summary>Olcum ve analiz amacli mesajlar.</summary>
        Analytics = 2
    }
}
