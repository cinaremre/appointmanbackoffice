using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Anonim takip olayı. Append-only'dir: güncelleme/silme metodu taşınmaz.
    /// Kişisel veri (isim, telefon, IP) içermez; oturum korelasyonu
    /// istemciden opsiyonel olarak gelen AnonymousSessionId ile yapılır.
    /// </summary>
    public class TrackingEvent : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Olayı tetikleyen takip linki; slug erişimlerinde boş olabilir.</summary>
        public Guid? TrackingLinkId { get; private set; }

        /// <summary>İstemciden opsiyonel oturum kimliği; kişisel veri içermez.</summary>
        public Guid? AnonymousSessionId { get; private set; }

        public EnumTrackingEventType EventType { get; private set; }

        /// <summary>Randevu ilişkili olaylarda randevu ID'si.</summary>
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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (appointmentId == Guid.Empty)
                throw new ArgumentException("Randevu ID boş olamaz.", nameof(appointmentId));

            BusinessId = businessId;
            EventType = eventType;
            TrackingLinkId = trackingLinkId;
            AnonymousSessionId = anonymousSessionId;
            AppointmentId = appointmentId;
            OccurredAt = DateTime.UtcNow;
        }
    }
}
