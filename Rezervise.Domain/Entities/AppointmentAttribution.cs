using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Bir randevunun pazarlama kaynağına atfi.
    /// Bir randevu için tek aktif atıf kaydı vardır; çift atıf
    /// handler tarafından Conflict ile engellenir (DuplicateKey koruması).
    /// </summary>
    public class AppointmentAttribution : BaseEntity
    {
        public Guid AppointmentId { get; private set; }
        public Guid BusinessId { get; private set; }

        public Guid? CampaignId { get; private set; }
        public Guid? TrackingLinkId { get; private set; }

        /// <summary>Bilinen ilk temas kaynağı (qr/whatsapp/ınstagram vb.).</summary>
        public string? FirstTouchSource { get; private set; }

        /// <summary>Son temas kaynağı.</summary>
        public string? LastTouchSource { get; private set; }

        public EnumAttributionModel AttributionModel { get; private set; }

        /// <summary>Atfin yapıldığı an.</summary>
        public DateTime AttributedAt { get; private set; }

        private AppointmentAttribution() { }

        public AppointmentAttribution(
            Guid appointmentId,
            Guid businessId,
            EnumAttributionModel attributionModel,
            Guid? campaignId = null,
            Guid? trackingLinkId = null,
            string? firstTouchSource = null,
            string? lastTouchSource = null)
        {
            if (appointmentId == Guid.Empty)
                throw new ArgumentException("Randevu ID boş olamaz.", nameof(appointmentId));

            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (campaignId == Guid.Empty)
                throw new ArgumentException("Kampanya ID boş olamaz.", nameof(campaignId));

            if (trackingLinkId == Guid.Empty)
                throw new ArgumentException("Takip linki ID boş olamaz.", nameof(trackingLinkId));

            SetSource(ref firstTouchSource);
            SetSource(ref lastTouchSource);

            AppointmentId = appointmentId;
            BusinessId = businessId;
            CampaignId = campaignId;
            TrackingLinkId = trackingLinkId;
            FirstTouchSource = firstTouchSource;
            LastTouchSource = lastTouchSource;
            AttributionModel = attributionModel;
            AttributedAt = DateTime.UtcNow;
        }

        /// <summary>Ortak kaynak etiketi doğrulaması (maks. 50 karakter).</summary>
        private static void SetSource(ref string? source)
        {
            if (source != null && source.Length > 50)
                throw new DomainException("Attribution.SourceTooLong", "Kaynak etiketi en fazla 50 karakter olabilir.");

            source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        }
    }
}
