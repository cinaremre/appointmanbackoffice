using System.Globalization;
using System.Text.Json;
using Rezervise.Application.Common.Interfaces;

namespace Rezervise.Api.Localization
{
    /// <summary>
    /// JSON dosyalarindan beslenen lokalizasyon servisi.
    /// Kataloglar uygulama baslangicinda bir kez yuklenir ve bellekte tutulur;
    /// runtime dosya izleme yapilmaz, degisiklik icin uygulama yeniden baslatilir.
    /// </summary>
    public sealed class JsonLocalizationService : ILocalizationService
    {
        private const string FallbackCultureName = "tr";
        private const string ResourceFolderName = "Localization";
        private const string ResourcesFolderName = "Resources";

        private static readonly string[] SupportedCultureNames = { "tr", "en" };

        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _catalogs;

        /// <summary>
        /// Kataloglari bellek yukler. Dosya eksik veya bozuksa uygulama durdurulmaz;
        /// ilgili kultur icin bos katalog kullanilir ve cozumleme anahtari dondurur.
        /// </summary>
        /// <param name="environment">Dosya yolunu cozmek icin host ortami.</param>
        /// <param name="logger">Uyari/hata kaydi.</param>
        public JsonLocalizationService(IHostEnvironment environment, ILogger<JsonLocalizationService> logger)
        {
            _catalogs = LoadCatalogs(environment, logger);
            WarnOnKeyParityMismatch(logger);
        }

        /// <inheritdoc />
        public CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;

        /// <inheritdoc />
        public string Get(string key) => Resolve(key, CurrentCulture.Name);

        /// <inheritdoc />
        public string Get(string key, params object[] args)
        {
            var template = Get(key);

            if (args is null || args.Length == 0)
                return template;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException)
            {
                // Sablon ile arguman sayisi uyusmuyorsa ham metin doner;
                // kullaniciya format hatasi sizdirilmaz.
                return template;
            }
        }

        /// <inheritdoc />
        public string Get(string key, CultureInfo culture) => Resolve(key, culture?.Name);

        /// <inheritdoc />
        public bool HasKey(string key, string cultureName)
        {
            var normalized = NormalizeCultureName(cultureName) ?? FallbackCultureName;

            return _catalogs.TryGetValue(normalized, out var catalog)
                   && catalog.ContainsKey(key);
        }

        // ===== Fallback zinciri (tek uygulama noktasi) =====
        /// <summary>
        /// Aktif kultur -> varsayilan kultur (tr) -> anahtarin kendisi.
        /// Hicbir asamada exception firlatilmaz.
        /// </summary>
        private string Resolve(string key, string? cultureName)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            var normalized = NormalizeCultureName(cultureName);

            if (normalized is not null
                && _catalogs.TryGetValue(normalized, out var catalog)
                && catalog.TryGetValue(key, out var value))
            {
                return value;
            }

            if (_catalogs.TryGetValue(FallbackCultureName, out var fallback)
                && fallback.TryGetValue(key, out var fallbackValue))
            {
                return fallbackValue;
            }

            return key;
        }

        private static string? NormalizeCultureName(string? cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
                return null;

            var trimmed = cultureName.Trim();

            // "tr-TR" gibi bolgesel adlar iki harfli kulture indirgenir.
            if (trimmed.Length >= 2 && char.IsLetter(trimmed[0]) && char.IsLetter(trimmed[1]))
                return trimmed[..2].ToLowerInvariant();

            return trimmed.ToLowerInvariant();
        }

        private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadCatalogs(
            IHostEnvironment environment,
            ILogger logger)
        {
            var catalogs = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var cultureName in SupportedCultureNames)
            {
                var path = ResolveCatalogPath(environment, cultureName);

                if (path is null)
                {
                    logger.LogWarning(
                        "Lokalizasyon dosyasi bulunamadi: {Culture}. Mesajlar anahtar olarak donecek.",
                        cultureName);

                    catalogs[cultureName] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    continue;
                }

                try
                {
                    using var stream = File.OpenRead(path);
                    using var document = JsonDocument.Parse(stream);

                    catalogs[cultureName] = Flatten(document.RootElement, string.Empty);

                    logger.LogInformation(
                        "Lokalizasyon dosyasi yuklendi: {Culture} ({Count} anahtar).",
                        cultureName,
                        catalogs[cultureName].Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lokalizasyon dosyasi okunamadi: {Path}", path);

                    catalogs[cultureName] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }

            return catalogs;
        }

        private static string? ResolveCatalogPath(IHostEnvironment environment, string cultureName)
        {
            var fileName = $"{cultureName}.json";

            var candidates = new[]
            {
                // Gelistirme: proje kok dizini
                Path.Combine(environment.ContentRootPath, ResourcesFolderName, ResourceFolderName, fileName),
                // Yayinlanmis cikti
                Path.Combine(AppContext.BaseDirectory, ResourcesFolderName, ResourceFolderName, fileName)
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        /// <summary>
        /// Ic ice JSON yapisini "Alan.AltAlan.Anahtar" biciminde duz sozluge indirger.
        /// </summary>
        private static IReadOnlyDictionary<string, string> Flatten(JsonElement element, string prefix)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in element.EnumerateObject())
            {
                var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.Object:
                        foreach (var nested in Flatten(property.Value, key))
                            result[nested.Key] = nested.Value;
                        break;

                    case JsonValueKind.String:
                        result[key] = property.Value.GetString() ?? string.Empty;
                        break;
                }
            }

            return result;
        }

        private void WarnOnKeyParityMismatch(ILogger logger)
        {
            if (!_catalogs.TryGetValue(FallbackCultureName, out var reference) || reference.Count == 0)
                return;

            foreach (var (cultureName, catalog) in _catalogs)
            {
                if (string.Equals(cultureName, FallbackCultureName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var missing = reference.Keys.Where(key => !catalog.ContainsKey(key)).ToList();

                if (missing.Count > 0)
                {
                    logger.LogWarning(
                        "Lokalizasyon katalogunda eksik anahtarlar: {Culture} ({Count}) -> {Keys}",
                        cultureName,
                        missing.Count,
                        string.Join(", ", missing.Take(20)));
                }
            }
        }
    }
}
