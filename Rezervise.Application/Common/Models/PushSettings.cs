using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Rezervise.Application.Common.Models
{
    /// <summary>
    /// "Push" yapilandirma bolumunden okunan FCM ayarlari.
    /// ServiceAccount dosyasi YALNIZCA sunucuda saklanir; repoya ASLA girmez.
    /// </summary>
    public sealed class PushSettings
    {
        /// <summary>Yapilandirma bolum adi.</summary>
        public const string SectionName = "Push";

        /// <summary>FCM gonderimleri yapilsin mi? false ise log tasimasi kullanilir.</summary>
        public bool Enabled { get; set; }

        /// <summary>Firebase proje kimligi (FCM HTTP v1 icin zorunlu).</summary>
        public string? ProjectId { get; set; }

        /// <summary>
        /// Firebase service account JSON dosyasinin sunucudaki yolu.
        /// Console'da "Generate new private key" ile indirilir.
        /// </summary>
        public string? ServiceAccountPath { get; set; }

        /// <summary>Kuyruk kapasitesi; doluysa yeni bildirim reddedilir ve loglanir.</summary>
        public int QueueCapacity { get; set; } = 500;

        /// <summary>FCM gonderimi icin gereken yapilandirma tam mi?</summary>
        public bool HasFcmConfiguration =>
            Enabled
            && !string.IsNullOrWhiteSpace(ProjectId)
            && !string.IsNullOrWhiteSpace(ServiceAccountPath);

        /// <summary>
        /// Yapilandirmadan ayarlari okur; eksik degerlerde log tasimasi devrededir.
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
