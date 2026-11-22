using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Isletme bazinda ozellik erisimini gecici olarak degistiren yonetici karari
    /// (dokuman Modul 14). Aktif override abonelik planindaki degerleri ezer.
    /// </summary>
    public class BusinessFeatureOverride : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid FeatureId { get; private set; }
        public bool IsEnabled { get; private set; }

        /// <summary>Null ise plan kotasi gecerli kalir.</summary>
        public int? QuotaOverride { get; private set; }

        public DateTime ValidFrom { get; private set; }

        /// <summary>UTC gecerlilik sonu; null = belirsiz sure.</summary>
        public DateTime? ValidUntil { get; private set; }

        public string? Reason { get; private set; }

        /// <summary>Karari veren platform yoneticisi.</summary>
        public Guid GrantedBy { get; private set; }

        // Navigation Properties
        public Business Business { get; private set; }
        public FeatureDefinition Feature { get; private set; }

        private BusinessFeatureOverride() { } // EF Core

        public BusinessFeatureOverride(
            Guid businessId,
            Guid featureId,
            bool isEnabled,
            int? quotaOverride,
            DateTime validFrom,
            DateTime? validUntil,
            string? reason,
            Guid grantedBy)
            : this(Guid.NewGuid(), businessId, featureId, isEnabled, quotaOverride, validFrom, validUntil, reason, grantedBy)
        {
        }

        public BusinessFeatureOverride(
            Guid id,
            Guid businessId,
            Guid featureId,
            bool isEnabled,
            int? quotaOverride,
            DateTime validFrom,
            DateTime? validUntil,
            string? reason,
            Guid grantedBy)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (featureId == Guid.Empty)
                throw new ArgumentException("Ozellik ID bos olamaz.", nameof(featureId));

            if (grantedBy == Guid.Empty)
                throw new ArgumentException("Yetki veren kullanici ID bos olamaz.", nameof(grantedBy));

            if (quotaOverride.HasValue && quotaOverride.Value < 0)
                throw new DomainException("Kota override negatif olamaz.");

            validFrom = DateTime.SpecifyKind(validFrom, DateTimeKind.Utc);

            if (validUntil.HasValue)
            {
                validUntil = DateTime.SpecifyKind(validUntil.Value, DateTimeKind.Utc);

                if (validUntil.Value <= validFrom)
                    throw new DomainException("Gecerlilik sonu baslangictan sonra olmali.");
            }

            if (reason != null && reason.Length > 500)
                throw new DomainException("Gerekce 500 karakterden uzun olamaz.");

            BusinessId = businessId;
            FeatureId = featureId;
            IsEnabled = isEnabled;
            QuotaOverride = quotaOverride;
            ValidFrom = validFrom;
            ValidUntil = validUntil;
            Reason = reason;
            GrantedBy = grantedBy;
        }

        /// <summary>
        /// Verilen UTC anda override gecerli mi?
        /// now [ValidFrom, ValidUntil) araligina dusmeli.
        /// </summary>
        public bool IsActiveAt(DateTime utc)
        {
            return !IsDeleted
                && ValidFrom <= utc
                && (ValidUntil is null || utc < ValidUntil.Value);
        }
    }
}
