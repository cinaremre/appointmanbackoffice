using System.Globalization;
using Rezervise.Application.Common.Interfaces;

namespace Rezervise.BackOffice.Api.Configuration
{
    /// <summary>
    /// Backoffice için minimal lokalizasyon hizmeti. Admin uçları
    /// lokalizasyona bağlı değildir; MediatR validatör'larının DI
    /// bağımlılığını karşılar ve anahtarı öldüğü gibi döndürür.
    /// </summary>
    public sealed class BackOfficeLocalizationService : ILocalizationService
    {
        public CultureInfo CurrentCulture { get; set; } = new("tr-TR");

        public string Get(string key) => key;

        public string Get(string key, params object[] args) => key;

        public string Get(string key, CultureInfo culture) => key;

        public bool HasKey(string key, string cultureName) => false;
    }
}
