using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace AppointMan.Application.Common.Models
{
    /// <summary>
    /// "Email" yapilandirma bolumunden okunan SMTP ve dogrulama bagi ayarlari.
    /// Ortam degiskenleri (Email__Host gibi) appsettings degerlerini ezer.
    /// </summary>
    public sealed class EmailOptions
    {
        /// <summary>Yapilandirma bolum adi.</summary>
        public const string SectionName = "Email";

        /// <summary>SMTP sunucu adresi. Bos ise log moduna dusulur.</summary>
        public string? Host { get; set; }

        /// <summary>SMTP portu.</summary>
        public int Port { get; set; } = 587;

        /// <summary>SSL kullanilacak mi.</summary>
        public bool UseSsl { get; set; } = true;

        /// <summary>SMTP kullanici adi. Bos ise kimlik dogrulama yapilmaz.</summary>
        public string? UserName { get; set; }

        /// <summary>SMTP sifresi. Hicbir zaman log'a yazilmaz.</summary>
        public string? Password { get; set; }

        /// <summary>Gonderen adresi.</summary>
        public string FromAddress { get; set; } = "no-reply@rezervise.com";

        /// <summary>Gonderen gorunen adi.</summary>
        public string FromName { get; set; } = "AppointMan";

        /// <summary>
        /// Dogrulama endpoint'inin tam adresi (orn. https://host/api/Auth/verify-email).
        /// Bag olusturulurken token bu adrese eklenir.
        /// </summary>
        public string VerificationLinkBaseUrl { get; set; } = string.Empty;

        /// <summary>Dogrulama token'inin gecerlilik suresi (saat).</summary>
        public int TokenLifetimeHours { get; set; } = 24;

        /// <summary>Yeniden dogrulama e-postasi istemek icin beklenecek sure (dakika).</summary>
        public int ResendCooldownMinutes { get; set; } = 2;

        /// <summary>Kuyruk kapasitesi. Dolu oldugunda mesaj reddedilir ve loglanir.</summary>
        public int QueueCapacity { get; set; } = 500;

        /// <summary>SMTP gonderim zaman asimi (milisaniye).</summary>
        public int SmtpTimeoutMilliseconds { get; set; } = 15000;

        /// <summary>
        /// Yapilandirmadan ayarlari okur. Eksik veya hatali degerler varsayilana duser;
        /// yapilandirma hatasi uygulamayi durdurmaz.
        /// </summary>
        /// <param name="configuration">Uygulama yapilandirmasi.</param>
        /// <returns>Doldurulmus ayar nesnesi.</returns>
        /// <exception cref="ArgumentNullException">Yapilandirma null oldugunda firlatilir.</exception>
        public static EmailOptions FromConfiguration(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var defaults = new EmailOptions();

            return new EmailOptions
            {
                Host = ReadString(configuration, "Host"),
                Port = ReadInt(configuration, "Port", defaults.Port),
                UseSsl = ReadBool(configuration, "UseSsl", defaults.UseSsl),
                UserName = ReadString(configuration, "UserName"),
                Password = ReadString(configuration, "Password"),
                FromAddress = ReadString(configuration, "FromAddress") ?? defaults.FromAddress,
                FromName = ReadString(configuration, "FromName") ?? defaults.FromName,
                VerificationLinkBaseUrl = ReadString(configuration, "VerificationLinkBaseUrl") ?? string.Empty,
                TokenLifetimeHours = ReadInt(configuration, "TokenLifetimeHours", defaults.TokenLifetimeHours),
                ResendCooldownMinutes = ReadInt(configuration, "ResendCooldownMinutes", defaults.ResendCooldownMinutes),
                QueueCapacity = ReadInt(configuration, "QueueCapacity", defaults.QueueCapacity),
                SmtpTimeoutMilliseconds = ReadInt(configuration, "SmtpTimeoutMilliseconds", defaults.SmtpTimeoutMilliseconds)
            };
        }

        /// <summary>SMTP ile gercek gonderim yapilabilir mi.</summary>
        public bool HasSmtpConfiguration => !string.IsNullOrWhiteSpace(Host);

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

        private static bool ReadBool(IConfiguration configuration, string key, bool fallback)
        {
            var value = configuration[$"{SectionName}:{key}"];
            return bool.TryParse(value, out var parsed) ? parsed : fallback;
        }
    }
}
