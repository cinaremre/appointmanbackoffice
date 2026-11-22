namespace Rezervise.Domain.Common
{
    /// <summary>
    /// Sistemde tanimlanabilir tum ozelliklerin kodlari (dokuman Modul 14).
    /// Kodlar FeatureDefinition.Code alaninda benzersiz olarak saklanir;
    /// uygulama kodu magic string yerine bu sabitleri kullanir.
    /// </summary>
    public static class FeatureCodes
    {
        /// <summary>Randevu temel islemleri (olusturma, gorme, iptal).</summary>
        public const string AppointmentBasic = "APPOINTMENT_BASIC";

        /// <summary>WhatsApp uzerinden hatirlatma gonderimi.</summary>
        public const string WhatsAppReminder = "WHATSAPP_REMINDER";

        /// <summary>SMS uzerinden hatirlatma gonderimi.</summary>
        public const string SmsReminder = "SMS_REMINDER";

        /// <summary>Bekleme listesi otomasyonu.</summary>
        public const string WaitlistAutomation = "WAITLIST_AUTOMATION";

        /// <summary>Geri cagirma (reengagement) otomasyonu.</summary>
        public const string ReengagementAutomation = "REENGAGEMENT_AUTOMATION";

        /// <summary>Gelismis raporlama ve analiz paneli.</summary>
        public const string AdvancedReporting = "ADVANCED_REPORTING";

        /// <summary>Stok yonetimi.</summary>
        public const string StockManagement = "STOCK_MANAGEMENT";

        /// <summary>Kasa yonetimi.</summary>
        public const string CashManagement = "CASH_MANAGEMENT";

        /// <summary>Komisyon yonetimi.</summary>
        public const string CommissionManagement = "COMMISSION_MANAGEMENT";

        /// <summary>Hizmet paketleri.</summary>
        public const string ServicePackages = "SERVICE_PACKAGES";

        /// <summary>Pazarlama atif (attribution) takibi.</summary>
        public const string MarketingAttribution = "MARKETING_ATTRIBUTION";

        /// <summary>Veri import araci.</summary>
        public const string DataImport = "DATA_IMPORT";

        /// <summary>
        /// Abonelik kaydi olmayan isletmelerde varsayilan olarak ACIK
        /// olan temel ozellikler (dokuman 14.4 "varsayilan ucretsiz plan").
        /// </summary>
        public static readonly string[] BasicDefaults = { AppointmentBasic };
    }
}
