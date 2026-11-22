namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Cihaz platformu. FCM tek API'den her iki platforma da gonderim yapar.
    /// </summary>
    public enum EnumDevicePlatform
    {
        /// <summary>Android (FCM yerlesik).</summary>
        Android = 0,

        /// <summary>iOS (APNs, FCM uzerinden yonetilir).</summary>
        Ios = 1
    }
}
