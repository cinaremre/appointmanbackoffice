namespace Rezervise.Domain.Common
{
    /// <summary>
    /// Sistemde tanımlanabilir tüm özelliklerin kodları (doküman Modül 14).
    /// Kodlar FeatureDefinition.Code alanında benzersiz olarak saklanır;
    /// uygulama kodu magic string yerine bu sabitleri kullanır.
    /// </summary>
    public static class FeatureCodes
    {
        /// <summary>Randevu temel işlemleri (oluşturma, görme, iptal).</summary>
        public const string AppointmentBasic = "APPOINTMENT_BASIC";

        /// <summary>WhatsApp üzerinden hatırlatma gönderimi.</summary>
        public const string WhatsAppReminder = "WHATSAPP_REMINDER";

        /// <summary>SMS üzerinden hatırlatma gönderimi.</summary>
        public const string SmsReminder = "SMS_REMINDER";

        /// <summary>Bekleme listesi otomasyonu.</summary>
        public const string WaitlistAutomation = "WAITLIST_AUTOMATION";

        /// <summary>Geri çağırma (reengagement) otomasyonu.</summary>
        public const string ReengagementAutomation = "REENGAGEMENT_AUTOMATION";

        /// <summary>Gelişmiş raporlama ve analiz paneli.</summary>
        public const string AdvancedReporting = "ADVANCED_REPORTING";

        /// <summary>Stok yönetimi.</summary>
        public const string StockManagement = "STOCK_MANAGEMENT";

        /// <summary>Kasa yönetimi.</summary>
        public const string CashManagement = "CASH_MANAGEMENT";

        /// <summary>Komisyon yönetimi.</summary>
        public const string CommissionManagement = "COMMISSION_MANAGEMENT";

        /// <summary>Hizmet paketleri.</summary>
        public const string ServicePackages = "SERVICE_PACKAGES";

        /// <summary>Pazarlama atıf (attribution) takibi.</summary>
        public const string MarketingAttribution = "MARKETING_ATTRIBUTION";

        /// <summary>Veri import aracı.</summary>
        public const string DataImport = "DATA_IMPORT";

        /// <summary>
        /// Abonelik kaydı olmayan işletmelerde varsayılan olarak ACIK
        /// olan temel özellikler (doküman 14.4 "varsayılan ücretsiz plan").
        /// </summary>
        public static readonly string[] BasicDefaults = { AppointmentBasic };
    }
}
