using System.Globalization;

namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Anahtar tabanlı metin çözümleyici.
    /// Domain katmanına bağımlılık oluşturmaz; yalnızca Application ve Api
    /// katmanlarında kullanılır. Anahtar bulunamazsa exception fırlatmaz,
    /// sırayla aktif kültür, varsayılan kültür (tr) ve anahtarın kendisine düşer.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// Geçerli istek kültürü. RequestLocalization tarafından belirlenir.
        /// </summary>
        CultureInfo CurrentCulture { get; }

        /// <summary>
        /// Anahtarı geçerli istek kültürüne göre çözer.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtarı (orn. "Appointment.DateInPast").</param>
        /// <returns>Çözülen metin; anahtar hiçbir katalogda yoksa anahtarın kendisi.</returns>
        string Get(string key);

        /// <summary>
        /// Anahtarı çözer ve metindeki {0}, {1} yer tutucularını verilen
        /// argümanlarla doldurur. Biçimlendirme hatası olursa ham şablon döner.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtarı.</param>
        /// <param name="args">Yer tutucu değerleri.</param>
        /// <returns>Çözülen ve biçimlendirilen metin.</returns>
        string Get(string key, params object[] args);

        /// <summary>
        /// Anahtarı belirtilen kültür için çözer (teşhis ve test amaçlı).
        /// </summary>
        /// <param name="key">Lokalizasyon anahtarı.</param>
        /// <param name="culture">Hedef kültür.</param>
        /// <returns>Çözülen metin; bulunamazsa fallback zinciri uygulanır.</returns>
        string Get(string key, CultureInfo culture);

        /// <summary>
        /// Anahtarın belirtilen kültür kataloğunda tanımlı olup olmadığını bildirir.
        /// Fallback uygulamaz.
        /// </summary>
        /// <param name="key">Lokalizasyon anahtarı.</param>
        /// <param name="cultureName">Kültür adı (orn. "tr", "en").</param>
        /// <returns>Anahtar katalogda tanımlıysa true.</returns>
        bool HasKey(string key, string cultureName);
    }
}
