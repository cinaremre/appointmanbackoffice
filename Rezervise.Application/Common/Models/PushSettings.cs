using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Rezervise.Application.Common.Models
{
    /// <summary>
    /// "Push" yapılandırma bölümünden okunan FCM ayarları.
    /// ServiceAccount dosyası YALNIZCA sunucuda saklanır; repoya ASLA girmez.
    /// </summary>
    public sealed class PushSettings
    {
        /// <summary>Yapılandırma bölüm adı.</summary>
        public const string SectionName = "Push";

        /// <summary>FCM gönderimleri yapılsın mi? false ise log taşıması kullanılır.</summary>
        public bool Enabled { get; set; }

        /// <summary>Firebase proje kimliği (FCM HTTP v1 için zorunlu).</summary>
        public string? ProjectId { get; set; }

        /// <summary>
        /// Firebase service account JSON dosyasının sunucudaki yolu.
        /// Console'da "Generate new private key" ile indirilir.
        /// </summary>
        public string? ServiceAccountPath { get; set; }

        /// <summary>Kuyruk kapasitesi; doluysa yeni bildirim reddedilir ve loğlanır.</summary>
        public int QueueCapacity { get; set; } = 500;

        /// <summary>FCM gönderimi için gereken yapılandırma tam mi?</summary>
        public bool HasFcmConfiguration =>
            Enabled
            && !string.IsNullOrWhiteSpace(ProjectId)
            && !string.IsNullOrWhiteSpace(ServiceAccountPath);

        /// <summary>
        /// Yapılandırmadan ayarları okur; eksik değerlerde log taşıması devrededir.
        /// </summary>
        public static PushSettings FromConfiguration(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            return new PushSettings
            {
                Enabled = ReadBool(configuration, "Enabled"),
                ProjectId = ReadString(configuration, "ProjectId"),
                ServiceAccountPath = ReadString(configuration, "ServiceAccountPath"),
                QueueCapacity = ReadInt(configuration, "QueueCapacity", 500)
            };
        }

        private static string? ReadString(IConfiguration configuration, string key)
        {
            var value = configuration[$"{SectionName}:{key}"];
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool ReadBool(IConfiguration configuration, string key)
        {
            var value = configuration[$"{SectionName}:{key}"];
            return bool.TryParse(value, out var parsed) && parsed;
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
