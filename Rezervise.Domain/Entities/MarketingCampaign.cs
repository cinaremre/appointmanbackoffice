using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Pazarlama kampanyası (Instagram, Meta Ads, Google Ads vb.).
    /// Harcama ve randevu atfi kayıtları bu kampanyaya bağlanır.
    /// </summary>
    public class MarketingCampaign : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public string Name { get; private set; }
        public EnumCampaignChannel Channel { get; private set; }

        /// <summary>Reklam platformundaki harici kampanya ID'si (opsiyonel).</summary>
        public string? ExternalCampaignId { get; private set; }

        public DateTime? StartAt { get; private set; }
        public DateTime? EndAt { get; private set; }
        public bool IsActive { get; private set; }

        private MarketingCampaign() { }

        public MarketingCampaign(
            Guid businessId,
            string name,
            EnumCampaignChannel channel,
            string? externalCampaignId = null,
            DateTime? startAt = null,
            DateTime? endAt = null)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            BusinessId = businessId;
            SetName(name);
            SetExternalCampaignId(externalCampaignId);
            SetPeriod(startAt, endAt);
            Channel = channel;
            IsActive = true;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Campaign.NameRequired", "Kampanya adı boş olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Campaign.NameLength", "Kampanya adı 2-100 karakter arasında olmalıdır.");

            Name = name.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetExternalCampaignId(string? externalCampaignId)
        {
            if (externalCampaignId != null && externalCampaignId.Length > 100)
                throw new DomainException("Campaign.ExternalIdTooLong",
                    "Harici kampanya ID en fazla 100 karakter olabilir.");

            ExternalCampaignId = string.IsNullOrWhiteSpace(externalCampaignId) ? null : externalCampaignId.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPeriod(DateTime? startAt, DateTime? endAt)
        {
            if (startAt.HasValue && endAt.HasValue && endAt.Value <= startAt.Value)
                throw new DomainException("Campaign.PeriodInvalid", "Kampanya bitiş tarihi başlangıçtan sonra olmalıdır.");

            StartAt = startAt;
            EndAt = endAt;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Kampanyayı pasifleştirir; yeni harcama/atıf almaz.</summary>
        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Kampanyayı yeniden aktifleştirir.</summary>
        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
