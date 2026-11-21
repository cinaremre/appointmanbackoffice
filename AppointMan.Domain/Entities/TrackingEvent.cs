using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Anonim takip olayi. Append-only'dir: guncelleme/silme metodu tasinmaz.
    /// Kisisel veri (isim, telefon, IP) icermez; oturum korelasyonu
    /// istemciden opsiyonel olarak gelen AnonymousSessionId ile yapilir.
    /// </summary>
    public class TrackingEvent : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Olayi tetikleyen takip linki; slug erisimlerinde bos olabilir.</summary>
        public Guid? TrackingLinkId { get; private set; }

        /// <summary>Istemciden opsiyonel oturum kimligi; kisisel veri icermez.</summary>
        public Guid? AnonymousSessionId { get; private set; }

        public EnumTrackingEventType EventType { get; private set; }

        /// <summary>Randevu iliskili olaylarda randevu ID'si.</summary>
        public Guid? AppointmentId { get; private set; }

        public DateTime OccurredAt { get; private set; }

        private TrackingEvent() { }

        public TrackingEvent(
            Guid businessId,
            EnumTrackingEventType eventType,
            Guid? trackingLinkId = null,
            Guid? anonymousSessionId = null,
            Guid? appointmentId = null)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (appointmentId == Guid.Empty)
                throw new ArgumentException("Randevu ID bos olamaz.", nameof(appointmentId));

            BusinessId = businessId;
            EventType = eventType;
            TrackingLinkId = trackingLinkId;
            AnonymousSessionId = anonymousSessionId;
            AppointmentId = appointmentId;
            OccurredAt = DateTime.UtcNow;
        }
    }
}
