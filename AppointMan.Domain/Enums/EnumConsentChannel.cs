namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Izin takibi yapilan iletisim kanallari (dokuman Modul 16).
    /// </summary>
    public enum EnumConsentChannel
    {
        /// <summary>SMS kanali.</summary>
        Sms = 0,

        /// <summary>E-posta kanali.</summary>
        Email = 1,

        /// <summary>WhatsApp kanali.</summary>
        WhatsApp = 2,

        /// <summary>Push bildirim kanali.</summary>
        Push = 3
    }
}
