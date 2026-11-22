using System.Text.RegularExpressions;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Isletmenin kamuya acik rezervasyon sayfasi.
    /// Slug tum sistemde benzersizdir; baska isletme ayni slug'i kullanamaz.
    /// QR kod gorseli frontend tarafinda uretilir; burada yalnizca slug
    /// uzerinden erisilebilir baglanti tanimlanir.
    /// </summary>
    public class BusinessPublicPage : BaseEntity
    {
        /// <summary>Slug izin verilen karakter kumesi: kucuk harf, rakam, tire.</summary>
        private static readonly Regex SlugRegex = new("^[a-z0-9-]+$", RegexOptions.Compiled);

        public Guid BusinessId { get; private set; }
        public string Slug { get; private set; }
        public bool IsPublished { get; private set; }

        /// <summary>Opsiyonel tema ayarlari (JSON metni). Frontend yorumlar.</summary>
        public string? ThemeSettings { get; private set; }

        private BusinessPublicPage() { }

        public BusinessPublicPage(Guid businessId, string slug, string? themeSettings = null, bool isPublished = true)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            BusinessId = businessId;
            SetSlug(slug);
            SetThemeSettings(themeSettings);
            IsPublished = isPublished;
        }

        /// <summary>
        /// Slug'i normalize eder ve kurala gore dogrular:
        /// 2-60 karakter, yalnizca kucuk harf/rakam/tire.
        /// </summary>
        public void SetSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new DomainException("PublicPage.SlugRequired", "Slug bos olamaz.");

            var normalized = slug.Trim().ToLowerInvariant();

            if (normalized.Length < 2 || normalized.Length > 60)
                throw new DomainException("PublicPage.SlugLength", "Slug 2-60 karakter arasinda olmalidir.");

            if (!SlugRegex.IsMatch(normalized))
                throw new DomainException("PublicPage.SlugInvalidChars",
                    "Slug yalnizca kucuk harf, rakam ve tire icerebilir.");

            Slug = normalized;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Tema ayarlarini (JSON) gunceller.</summary>
        public void SetThemeSettings(string? themeSettings)
        {
            if (themeSettings != null && themeSettings.Length > 2000)
                throw new DomainException("PublicPage.ThemeSettingsTooLong",
                    "Tema ayarlari 2000 karakterden uzun olamaz.");

            ThemeSettings = themeSettings;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Sayfayi yayina alir. Zaten yayindaysa degisiklik yapmaz.</summary>
        public void Publish()
        {
            if (IsPublished)
                return;

            IsPublished = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Sayfayi yayindan kaldirir. Zaten yayinda degilse degisiklik yapmaz.</summary>
        public void Unpublish()
        {
            if (!IsPublished)
                return;

            IsPublished = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
