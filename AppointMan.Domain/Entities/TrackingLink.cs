using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Isletmenin paylasilabilir takip linki (QR / WhatsApp / Instagram).
    /// TrackingCode kisa ve benzersizdir; public baglantida tasinir.
    /// QR gorseli frontend tarafinda uretilir; burada yalnizca kod ve
    /// hedef yol (DestinationPath) tutulur.
    /// </summary>
    public class TrackingLink : BaseEntity
    {
        /// <summary>Izin verilen kaynak etiketleri (UTM source benzeri).</summary>
        private static readonly string[] AllowedSources = { "qr", "whatsapp", "instagram" };

        public Guid BusinessId { get; private set; }
        public EnumTrackingTargetType TargetType { get; private set; }

        /// <summary>Hedefin varlik ID'si (Business tipinde bos birakilabilir).</summary>
        public Guid? TargetId { get; private set; }

        public string Source { get; private set; }
        public string? Medium { get; private set; }
        public string? Campaign { get; private set; }

        /// <summary>Benzersiz kisa takip kodu (8 karakter, hex).</summary>
        public string TrackingCode { get; private set; }

        /// <summary>Linkin yonlendirecegi goreli yol (orn. "/p/benim-salon").</summary>
        public string DestinationPath { get; private set; }

        public bool IsActive { get; private set; }

        private TrackingLink() { }

        public TrackingLink(
            Guid businessId,
            EnumTrackingTargetType targetType,
            string source,
            string destinationPath,
            Guid? targetId = null,
            string? medium = null,
            string? campaign = null,
            string? trackingCode = null)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (targetId == Guid.Empty)
                throw new ArgumentException("Hedef ID bos olamaz.", nameof(targetId));

            BusinessId = businessId;
            TargetType = targetType;
            TargetId = targetId;

            SetSource(source);
            SetMedium(medium);
            SetCampaign(campaign);
            SetDestinationPath(destinationPath);

            // Kod verilmediyse yeni benzersiz kisa kod uretilir.
            TrackingCode = string.IsNullOrWhiteSpace(trackingCode)
                ? NewTrackingCode()
                : trackingCode.Trim().ToLowerInvariant();

            IsActive = true;
        }

        /// <summary>Yeni 8 karakterli benzersiz takip kodu uretir.</summary>
        public static string NewTrackingCode()
            => Guid.NewGuid().ToString("N")[..8];

        private void SetSource(string source)
        {
            var normalized = source?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalized))
                throw new DomainException("TrackingLink.SourceRequired", "Takip linki kaynagi bos olamaz.");

            if (!AllowedSources.Contains(normalized))
                throw new DomainException("TrackingLink.SourceInvalid",
                    "Kaynak yalnizca qr, whatsapp veya instagram olabilir.");

            Source = normalized;
        }

        private void SetMedium(string? medium)
        {
            if (medium != null && medium.Length > 100)
                throw new DomainException("TrackingLink.MediumTooLong", "Medium en fazla 100 karakter olabilir.");

            Medium = string.IsNullOrWhiteSpace(medium) ? null : medium.Trim();
        }

        private void SetCampaign(string? campaign)
        {
            if (campaign != null && campaign.Length > 100)
                throw new DomainException("TrackingLink.CampaignTooLong", "Kampanya etiketi en fazla 100 karakter olabilir.");

            Campaign = string.IsNullOrWhiteSpace(campaign) ? null : campaign.Trim();
        }

        private void SetDestinationPath(string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new DomainException("TrackingLink.DestinationRequired", "Hedef yol bos olamaz.");

            if (destinationPath.Length > 500)
                throw new DomainException("TrackingLink.DestinationTooLong", "Hedef yol en fazla 500 karakter olabilir.");

            DestinationPath = destinationPath.Trim();
        }

        /// <summary>Linki kapatir; artik olay kabul etmez. Tekrar acma desteklenmez.</summary>
        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
