namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Anonim takip olayı tipleri. Kişisel veri taşınmaz;
    /// yalnızca AnonymousSessionId (istemciden opsiyonel) ile korelasyon yapılır.
    /// </summary>
    public enum EnumTrackingEventType
    {
        /// <summary>Link/sayfa ilk kez açıldı.</summary>
        LinkOpened = 0,

        /// <summary>Hizmet detayı görüntülendi.</summary>
        ServiceViewed = 1,

        /// <summary>Randevu talebi oluşturuldu.</summary>
        AppointmentRequested = 2,

        /// <summary>Randevu işletme tarafından onaylandı.</summary>
        AppointmentConfirmed = 3,

        /// <summary>Randevu gerçekleşti.</summary>
        AppointmentCompleted = 4,

        /// <summary>Ödeme alındı.</summary>
        PaymentReceived = 5
    }
}
