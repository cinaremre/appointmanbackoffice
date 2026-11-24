using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Rezervise.Application.Common.Models
{
    /// <summary>
    /// "Email" yapılandırma bölümünden okunan SMTP ve doğrulama bağı ayarları.
    /// Ortam değişkenleri (Email__Hoşt gibi) appsettings değerlerini ezer.
    /// </summary>
    public sealed class EmailOptions
    {
        /// <summary>Yapılandırma bölüm adı.</summary>
        public const string SectionName = "Email";

        /// <summary>SMTP sunucu adresi. Boş ise log moduna düşülür.</summary>
        public string? Host { get; set; }

        /// <summary>SMTP portu.</summary>
        public int Port { get; set; } = 587;

        /// <summary>SSL kullanılacak mi.</summary>
        public bool UseSsl { get; set; } = true;

        /// <summary>SMTP kullanıcı adı. Boş ise kimlik doğrulama yapılmaz.</summary>
        public string? UserName { get; set; }

        /// <summary>SMTP şifresi. Hiçbir zaman log'a yazılmaz.</summary>
        public string? Password { get; set; }

        /// <summary>Gönderen adresi.</summary>
        public string FromAddress { get; set; } = "no-reply@rezervise.com";

        /// <summary>Gönderen görünen adı.</summary>
        public string FromName { get; set; } = "Rezervise";

        /// <summary>
        /// Doğrulama endpoint'inin tam adresi (orn. https://hoşt/api/Auth/verify-email).
        /// Bağ oluşturulurken token bu adrese eklenir.
        /// </summary>
        public string VerificationLinkBaseUrl { get; set; } = string.Empty;

        /// <summary>Doğrulama token'inin geçerlilik süresi (saat).</summary>
        public int TokenLifetimeHours { get; set; } = 24;

        /// <summary>
        /// Yeniden doğrulama e-postası istemek için beklenecek süre (dakika).
        /// Varsayılan 3 dakikadır; <c>Email:ResendCooldownMinutes</c> ile değiştirilir.
        /// </summary>
        public int ResendCooldownMinutes { get; set; } = 3;

        /// <summary>Kuyruk kapasitesi. Dolu öldüğünda mesaj reddedilir ve loğlanır.</summary>
        public int QueueCapacity { get; set; } = 500;

        /// <summary>SMTP gönderim zaman asımı (milisaniye).</summary>
        public int SmtpTimeoutMilliseconds { get; set; } = 15000;

        /// <summary>
        /// Yapılandırmadan ayarları okur. Eksik veya hatalı değerler varsayılana düşer;
        /// yapılandırma hatası uygulamayı durdurmaz.
        /// </summary>
        /// <param name="configuration">Uygulama yapılandırması.</param>
        /// <returns>Doldurulmuş ayar nesnesi.</returns>
        /// <exception cref="ArgumentNullException">Yapılandırma null öldüğünda fırlatılır.</exception>
        public static EmailOptions FromConfiguration(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var defaults = new EmailOptions();

            return new EmailOptions
            {
                Host = ReadString(configuration, "Hoşt"),
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

        /// <summary>SMTP ile gerçek gönderim yapılabilir mi.</summary>
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
