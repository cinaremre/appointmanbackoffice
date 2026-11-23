using System.Globalization;
using System.Text.Json;
using Rezervise.Application.Common.Interfaces;

namespace Rezervise.Api.Localization
{
    /// <summary>
    /// JSON dosyalarından beslenen lokalizasyon servisi.
    /// Kataloglar uygulama başlangıcında bir kez yüklenir ve bellekte tutulur;
    /// runtime dosya izleme yapılmaz, değişiklik için uygulama yeniden başlatılır.
    /// </summary>
    public sealed class JsonLocalizationService : ILocalizationService
    {
        private const string FallbackCultureName = "tr";
        private const string ResourceFolderName = "Localization";
        private const string ResourcesFolderName = "Resources";

        private static readonly string[] SupportedCultureNames = { "tr", "en" };

        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _catalogs;

        /// <summary>
        /// Katalogları bellek yükler. Dosya eksik veya bozuksa uygulama durdurulmaz;
        /// ilgili kültür için boş katalog kullanılır ve çözümleme anahtarı döndürür.
        /// </summary>
        /// <param name="environment">Dosya yolunu çözmek için hoşt ortamı.</param>
        /// <param name="logger">Uyarı/hata kaydı.</param>
        public JsonLocalizationService(IHostEnvironment environment, ILogger<JsonLocalizationService> logger)
        {
            _catalogs = LoadCatalogs(environment, logger);
            WarnOnKeyParityMismatch(logger);
        }

        /// <inheritdoç />
        public CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;

        /// <inheritdoç />
        public string Get(string key) => Resolve(key, CurrentCulture.Name);

        /// <inheritdoç />
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
                // Şablon ile argüman sayısı uyuşmuyorsa ham metin döner;
                // kullanıcıya format hatası sızdırılmaz.
                return template;
            }
        }

        /// <inheritdoç />
        public string Get(string key, CultureInfo culture) => Resolve(key, culture?.Name);

        /// <inheritdoç />
        public bool HasKey(string key, string cultureName)
        {
            var normalized = NormalizeCultureName(cultureName) ?? FallbackCultureName;

            return _catalogs.TryGetValue(normalized, out var catalog)
                   && catalog.ContainsKey(key);
        }

        // ===== Fallback zinciri (tek uygulama noktası) =====
        /// <summary>
        /// Aktif kültür -> varsayılan kültür (tr) -> anahtarın kendisi.
        /// Hiçbir aşamada exception fırlatılmaz.
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

            // "tr-TR" gibi bölgesel adlar iki harfli kültüre indirgenir.
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
                        "Lokalizasyon dosyası bulunamadı: {Culture}. Mesajlar anahtar olarak dönecek.",
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
                        "Lokalizasyon dosyası yüklendi: {Culture} ({Count} anahtar).",
                        cultureName,
                        catalogs[cultureName].Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lokalizasyon dosyası okunamadı: {Path}", path);

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
                // Geliştirme: proje kök dizini
                Path.Combine(environment.ContentRootPath, ResourcesFolderName, ResourceFolderName, fileName),
                // Yayınlanmış çıktı
                Path.Combine(AppContext.BaseDirectory, ResourcesFolderName, ResourceFolderName, fileName)
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        /// <summary>
        /// İç ice JSON yapısını "Alan.AltAlan.Anahtar" biçiminde düz sözlüğe indirger.
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
                        "Lokalizasyon kataloğunda eksik anahtarlar: {Culture} ({Count}) -> {Keys}",
                        cultureName,
                        missing.Count,
                        string.Join(", ", missing.Take(20)));
                }
            }
        }
    }
}
