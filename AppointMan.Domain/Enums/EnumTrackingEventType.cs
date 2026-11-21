namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Anonim takip olayi tipleri. Kisisel veri tasinmaz;
    /// yalnizca AnonymousSessionId (istemciden opsiyonel) ile korelasyon yapilir.
    /// </summary>
    public enum EnumTrackingEventType
    {
        /// <summary>Link/sayfa ilk kez acildi.</summary>
        LinkOpened = 0,

        /// <summary>Hizmet detayi goruntulendi.</summary>
        ServiceViewed = 1,

        /// <summary>Randevu talebi olusturuldu.</summary>
        AppointmentRequested = 2,

        /// <summary>Randevu isletme tarafindan onaylandi.</summary>
        AppointmentConfirmed = 3,

        /// <summary>Randevu gerceklesti.</summary>
        AppointmentCompleted = 4,

        /// <summary>Odeme alindi.</summary>
        PaymentReceived = 5
    }
}
