using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Rezervise.Application.Common.Models
{
    /// <summary>
    /// "Otp" yapılandırma bölümünden okunan telefon doğrulama ayarları.
    /// Ortam değişkenleri (Otp__FixedCode gibi) appsettings değerlerini ezer.
    /// </summary>
    public sealed class OtpOptions
    {
        /// <summary>Yapılandırma bölüm adı.</summary>
        public const string SectionName = "Otp";

        /// <summary>
        /// Sabit kod. SMS sağlayıcı bağlanana kadar geliştirme için kullanılır;
        /// dolu öldüğünda üretilen ve kabul edilen kod budur. Üretimde boş bırakılmalıdır.
        /// </summary>
        public string? FixedCode { get; set; }

        /// <summary>Üretilen kodun uzunluğu.</summary>
        public int CodeLength { get; set; } = 6;

        /// <summary>Kodun geçerlilik süresi (dakika).</summary>
        public int LifetimeMinutes { get; set; } = 5;

        /// <summary>Maksimum doğrulama denemesi.</summary>
        public int MaxAttempts { get; set; } = 5;

        /// <summary>
        /// Yeniden kod isteme için beklenecek süre (saniye). Varsayılan 180 saniye
        /// (3 dakika); <c>Otp:ResendCooldownSeconds</c> ile değiştirilir.
        /// </summary>
        public int ResendCooldownSeconds { get; set; } = 180;

        /// <summary>Sabit kod yapılandırılmış mi?</summary>
        public bool HasFixedCode => !string.IsNullOrWhiteSpace(FixedCode);

        /// <summary>
        /// Yapılandırmadan ayarları okur. Eksik veya hatalı değerler varsayılana düşer;
        /// yapılandırma hatası uygulamayı durdurmaz.
        /// </summary>
        /// <param name="configuration">Uygulama yapılandırması.</param>
        /// <returns>Doldurulmuş ayar nesnesi.</returns>
        /// <exception cref="ArgumentNullException">Yapılandırma null öldüğünda fırlatılır.</exception>
        public static OtpOptions FromConfiguration(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var defaults = new OtpOptions();

            return new OtpOptions
            {
                FixedCode = ReadString(configuration, "FixedCode"),
                CodeLength = ReadInt(configuration, "CodeLength", defaults.CodeLength),
                LifetimeMinutes = ReadInt(configuration, "LifetimeMinutes", defaults.LifetimeMinutes),
                MaxAttempts = ReadInt(configuration, "MaxAttempts", defaults.MaxAttempts),
                ResendCooldownSeconds = ReadInt(configuration, "ResendCooldownSeconds", defaults.ResendCooldownSeconds)
            };
        }

        private static string? ReadString(IConfiguration configuration, string key)
        {
            var value = configuration[$"{SectionName}:{key}"];
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static int ReadInt(IConfiguration configuration, string key, int fallback)
        {
            var value = configuration[$"{SectionName}:{key}"];
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
                ? parsed
                : fallback;
        }
    }
}
