using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İşletme bazında özellik erişimini geçici olarak değiştiren yönetici kararı
    /// (doküman Modül 14). Aktif override abonelik planındaki değerleri ezer.
    /// </summary>
    public class BusinessFeatureOverride : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid FeatureId { get; private set; }
        public bool IsEnabled { get; private set; }

        /// <summary>Null ise plan kotası geçerli kalır.</summary>
        public int? QuotaOverride { get; private set; }

        public DateTime ValidFrom { get; private set; }

        /// <summary>UTC geçerlilik sonu; null = belirsiz süre.</summary>
        public DateTime? ValidUntil { get; private set; }

        public string? Reason { get; private set; }

        /// <summary>Kararı veren platform yöneticisi.</summary>
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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (featureId == Guid.Empty)
                throw new ArgumentException("Özellik ID boş olamaz.", nameof(featureId));

            if (grantedBy == Guid.Empty)
                throw new ArgumentException("Yetki veren kullanıcı ID boş olamaz.", nameof(grantedBy));

            if (quotaOverride.HasValue && quotaOverride.Value < 0)
                throw new DomainException("Kota override negatif olamaz.");

            validFrom = DateTime.SpecifyKind(validFrom, DateTimeKind.Utc);

            if (validUntil.HasValue)
            {
                validUntil = DateTime.SpecifyKind(validUntil.Value, DateTimeKind.Utc);

                if (validUntil.Value <= validFrom)
                    throw new DomainException("Geçerlilik sonu başlangıçtan sonra olmalı.");
            }

            if (reason != null && reason.Length > 500)
                throw new DomainException("Gerekçe 500 karakterden uzun olamaz.");

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
        /// Verilen UTC anda override geçerli mi?
        /// now [ValidFrom, ValidUntil) aralığına düşmeli.
        /// </summary>
        public bool IsActiveAt(DateTime utc)
        {
            return !IsDeleted
                && ValidFrom <= utc
                && (ValidUntil is null || utc < ValidUntil.Value);
        }
    }
}
