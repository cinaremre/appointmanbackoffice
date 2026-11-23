namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// İletişim izninin kullanım amacı (doküman Modül 16).
    /// Amaç iznin kapsamını belirler: pazarlama izni başka bir amaç için
    /// kullanılamaz.
    /// </summary>
    public enum EnumConsentPurpose
    {
        /// <summary>Randevu bildirimi, hatırlatma gibi işlemsel mesajlar.</summary>
        Transactional = 0,

        /// <summary>Kampanya, geri çağırma gibi pazarlama mesajları.</summary>
        Marketing = 1,

        /// <summary>Ölçüm ve analiz amaçlı mesajlar.</summary>
        Analytics = 2
    }
}
