using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İşletmenin paylaşılabilir takip linki (QR / WhatsApp / Instagram).
    /// TrackingCode kısa ve benzersizdir; public bağlantıda taşınır.
    /// QR görseli frontend tarafında üretilir; burada yalnızca kod ve
    /// hedef yol (DestinationPath) tutulur.
    /// </summary>
    public class TrackingLink : BaseEntity
    {
        /// <summary>İzin verilen kaynak etiketleri (UTM source benzeri).</summary>
        private static readonly string[] AllowedSources = { "qr", "whatsapp", "ınstagram" };

        public Guid BusinessId { get; private set; }
        public EnumTrackingTargetType TargetType { get; private set; }

        /// <summary>Hedefin varlık ID'si (Business tipinde boş bırakılabilir).</summary>
        public Guid? TargetId { get; private set; }

        public string Source { get; private set; }
        public string? Medium { get; private set; }
        public string? Campaign { get; private set; }

        /// <summary>Benzersiz kısa takip kodu (8 karakter, hex).</summary>
        public string TrackingCode { get; private set; }

        /// <summary>Linkin yönlendireceği göreli yol (orn. "/p/benim-salon").</summary>
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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (targetId == Guid.Empty)
                throw new ArgumentException("Hedef ID boş olamaz.", nameof(targetId));

            BusinessId = businessId;
            TargetType = targetType;
            TargetId = targetId;

            SetSource(source);
            SetMedium(medium);
            SetCampaign(campaign);
            SetDestinationPath(destinationPath);

            // Kod verilmediyse yeni benzersiz kısa kod üretilir.
            TrackingCode = string.IsNullOrWhiteSpace(trackingCode)
                ? NewTrackingCode()
                : trackingCode.Trim().ToLowerInvariant();

            IsActive = true;
        }

        /// <summary>Yeni 8 karakterli benzersiz takip kodu üretir.</summary>
        public static string NewTrackingCode()
            => Guid.NewGuid().ToString("N")[..8];

        private void SetSource(string source)
        {
            var normalized = source?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalized))
                throw new DomainException("TrackingLink.SourceRequired", "Takip linki kaynağı boş olamaz.");

            if (!AllowedSources.Contains(normalized))
                throw new DomainException("TrackingLink.SourceInvalid",
                    "Kaynak yalnızca qr, whatsapp veya ınstagram olabilir.");

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
                throw new DomainException("TrackingLink.DestinationRequired", "Hedef yol boş olamaz.");

            if (destinationPath.Length > 500)
                throw new DomainException("TrackingLink.DestinationTooLong", "Hedef yol en fazla 500 karakter olabilir.");

            DestinationPath = destinationPath.Trim();
        }

        /// <summary>Linki kapatır; artık olay kabul etmez. Tekrar açma desteklenmez.</summary>
        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
