using System.Text.RegularExpressions;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İşletmenin kamuya açık rezervasyon sayfası.
    /// Slug tüm sistemde benzersizdir; başka işletme aynı slug'i kullanamaz.
    /// QR kod görseli frontend tarafında üretilir; burada yalnızca slug
    /// üzerinden erişilebilir bağlantı tanımlanır.
    /// </summary>
    public class BusinessPublicPage : BaseEntity
    {
        /// <summary>Slug izin verilen karakter kümesi: küçük harf, rakam, tire.</summary>
        private static readonly Regex SlugRegex = new("^[a-z0-9-]+$", RegexOptions.Compiled);

        public Guid BusinessId { get; private set; }
        public string Slug { get; private set; }
        public bool IsPublished { get; private set; }

        /// <summary>Opsiyonel tema ayarları (JSON metni). Frontend yorumlar.</summary>
        public string? ThemeSettings { get; private set; }

        private BusinessPublicPage() { }

        public BusinessPublicPage(Guid businessId, string slug, string? themeSettings = null, bool isPublished = true)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            BusinessId = businessId;
            SetSlug(slug);
            SetThemeSettings(themeSettings);
            IsPublished = isPublished;
        }

        /// <summary>
        /// Slug'i normalize eder ve kurala göre doğrular:
        /// 2-60 karakter, yalnızca küçük harf/rakam/tire.
        /// </summary>
        public void SetSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new DomainException("PublicPage.SlugRequired", "Slug boş olamaz.");

            var normalized = slug.Trim().ToLowerInvariant();

            if (normalized.Length < 2 || normalized.Length > 60)
                throw new DomainException("PublicPage.SlugLength", "Slug 2-60 karakter arasında olmalıdır.");

            if (!SlugRegex.IsMatch(normalized))
                throw new DomainException("PublicPage.SlugInvalidChars",
                    "Slug yalnızca küçük harf, rakam ve tire içerebilir.");

            Slug = normalized;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Tema ayarlarını (JSON) günceller.</summary>
        public void SetThemeSettings(string? themeSettings)
        {
            if (themeSettings != null && themeSettings.Length > 2000)
                throw new DomainException("PublicPage.ThemeSettingsTooLong",
                    "Tema ayarları 2000 karakterden uzun olamaz.");

            ThemeSettings = themeSettings;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Sayfayı yayına alır. Zaten yayındaysa değişiklik yapmaz.</summary>
        public void Publish()
        {
            if (IsPublished)
                return;

            IsPublished = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Sayfayı yayından kaldırır. Zaten yayında değilse değişiklik yapmaz.</summary>
        public void Unpublish()
        {
            if (!IsPublished)
                return;

            IsPublished = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
