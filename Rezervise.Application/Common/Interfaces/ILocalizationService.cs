using System.Globalization;

namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Anahtar tabanli metin cozumleyici.
    /// Domain katmanina bagimlilik olusturmaz; yalnizca Application ve Api
    /// katmanlarinda kullanilir. Anahtar bulunamazsa exception firlatmaz,
    /// sirayla aktif kultur, varsayilan kultur (tr) ve anahtarin kendisine duser.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// Gecerli istek kulturu. RequestLocalization tarafindan belirlenir.
        /// </summary>
        CultureInfo CurrentCulture { get; }

        /// <summary>
        /// Anahtari gecerli istek kulturune gore cozer.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtari (orn. "Appointment.DateInPast").</param>
        /// <returns>Cozulen metin; anahtar hicbir katalogda yoksa anahtarin kendisi.</returns>
        string Get(string key);

        /// <summary>
        /// Anahtari cozer ve metindeki {0}, {1} yer tutucularini verilen
        /// argumanlarla doldurur. Bicimlendirme hatasi olursa ham sablon doner.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtari.</param>
        /// <param name="args">Yer tutucu degerleri.</param>
        /// <returns>Cozulen ve bicimlendirilen metin.</returns>
        string Get(string key, params object[] args);

        /// <summary>
        /// Anahtari belirtilen kultur icin cozer (teshis ve test amacli).
        /// </summary>
        /// <param name="key">Lokalizasyon anahtari.</param>
        /// <param name="culture">Hedef kultur.</param>
        /// <returns>Cozulen metin; bulunamazsa fallback zinciri uygulanir.</returns>
        string Get(string key, CultureInfo culture);

        /// <summary>
        /// Anahtarin belirtilen kultur katalogunda tanimli olup olmadigini bildirir.
        /// Fallback uygulamaz.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtari.</param>
        /// <param name="cultureName">Kultur adi (orn. "tr", "en").</param>
        /// <returns>Anahtar katalogda tanimliysa true.</returns>
        bool HasKey(string key, string cultureName);
    }
}
