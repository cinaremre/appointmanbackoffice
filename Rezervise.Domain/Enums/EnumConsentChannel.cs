namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// İzin takibi yapılan iletişim kanalları (doküman Modül 16).
    /// </summary>
    public enum EnumConsentChannel
    {
        /// <summary>SMS kanalı.</summary>
        Sms = 0,

        /// <summary>E-posta kanalı.</summary>
        Email = 1,

        /// <summary>WhatsApp kanalı.</summary>
        WhatsApp = 2,

        /// <summary>Push bildirim kanalı.</summary>
        Push = 3
    }
}
