using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Rezervise.Application.Common.Models
{
    /// <summary>
    /// "Otp" yapilandirma bolumunden okunan telefon dogrulama ayarlari.
    /// Ortam degiskenleri (Otp__FixedCode gibi) appsettings degerlerini ezer.
    /// </summary>
    public sealed class OtpOptions
    {
        /// <summary>Yapilandirma bolum adi.</summary>
        public const string SectionName = "Otp";

        /// <summary>
        /// Sabit kod. SMS saglayici baglanana kadar gelistirme icin kullanilir;
        /// dolu oldugunda uretilen ve kabul edilen kod budur. Uretimde bos birakilmalidir.
        /// </summary>
        public string? FixedCode { get; set; }

        /// <summary>Uretilen kodun uzunlugu.</summary>
        public int CodeLength { get; set; } = 6;

        /// <summary>Kodun gecerlilik suresi (dakika).</summary>
        public int LifetimeMinutes { get; set; } = 5;

        /// <summary>Maksimum dogrulama denemesi.</summary>
        public int MaxAttempts { get; set; } = 5;

        /// <summary>Yeniden kod isteme icin beklenecek sure (saniye).</summary>
        public int ResendCooldownSeconds { get; set; } = 60;

        /// <summary>Sabit kod yapilandirilmis mi?</summary>
        public bool HasFixedCode => !string.IsNullOrWhiteSpace(FixedCode);

        /// <summary>
        /// Yapilandirmadan ayarlari okur. Eksik veya hatali degerler varsayilana duser;
        /// yapilandirma hatasi uygulamayi durdurmaz.
        /// </summary>
        /// <param name="configuration">Uygulama yapilandirmasi.</param>
        /// <returns>Doldurulmus ayar nesnesi.</returns>
        /// <exception cref="ArgumentNullException">Yapilandirma null oldugunda firlatilir.</exception>
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
