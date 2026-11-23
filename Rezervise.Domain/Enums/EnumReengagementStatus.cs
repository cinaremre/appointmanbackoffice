namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Geri çağırma (reengagement) denemesinin durumu (doküman Modül 13).
    /// </summary>
    public enum EnumReengagementStatus
    {
        /// <summary>Aday belirlendi, bildirim henüz gönderilmedi.</summary>
        Eligible = 0,

        /// <summary>Müşteriye bildirim gönderildi.</summary>
        Notified = 1,

        /// <summary>Müşteri geri çağırmayı randevuya çevirdi.</summary>
        Converted = 2,

        /// <summary>İzin/molal nedeniyle gönderim engellendi.</summary>
        Suppressed = 3,

        /// <summary>Cooldown süresi içinde; tekrar deneme bekliyor.</summary>
        Cooldown = 4
    }
}
