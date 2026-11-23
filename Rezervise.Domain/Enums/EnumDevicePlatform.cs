namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Cihaz platformu. FCM tek API'den her iki platforma da gönderim yapar.
    /// </summary>
    public enum EnumDevicePlatform
    {
        /// <summary>Android (FCM yerleşik).</summary>
        Android = 0,

        /// <summary>iOS (APNs, FCM üzerinden yönetilir).</summary>
        Ios = 1
    }
}
