namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Geri cagirma (reengagement) denemesinin durumu (dokuman Modul 13).
    /// </summary>
    public enum EnumReengagementStatus
    {
        /// <summary>Aday belirlendi, bildirim henuz gonderilmedi.</summary>
        Eligible = 0,

        /// <summary>Musteriye bildirim gonderildi.</summary>
        Notified = 1,

        /// <summary>Musteri geri cagirmayi randevuya cevirdi.</summary>
        Converted = 2,

        /// <summary>Izin/molal nedeniyle gonderim engellendi.</summary>
        Suppressed = 3,

        /// <summary>Cooldown suresi icinde; tekrar deneme bekliyor.</summary>
        Cooldown = 4
    }
}
