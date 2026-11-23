using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Bir abonelik planına dahil edilen özellik ve kotası (doküman Modül 14).
    /// IncludedQuota null ise özellik sınırsız kullanılabilir.
    /// </summary>
    public class PlanFeature : BaseEntity
    {
        public Guid PlanId { get; private set; }
        public Guid FeatureId { get; private set; }
        public bool IsEnabled { get; private set; }

        /// <summary>Dönemsel kullanım sınırı; null = sınırsız.</summary>
        public int? IncludedQuota { get; private set; }

        // Navigation Properties
        public SubscriptionPlan Plan { get; private set; }
        public FeatureDefinition Feature { get; private set; }

        private PlanFeature() { } // EF Core

        public PlanFeature(Guid planId, Guid featureId, bool isEnabled, int? includedQuota = null)
            : this(Guid.NewGuid(), planId, featureId, isEnabled, includedQuota)
        {
        }

        public PlanFeature(Guid id, Guid planId, Guid featureId, bool isEnabled, int? includedQuota = null)
            : base(id)
        {
            if (planId == Guid.Empty)
                throw new ArgumentException("Plan ID boş olamaz.", nameof(planId));

            if (featureId == Guid.Empty)
                throw new ArgumentException("Özellik ID boş olamaz.", nameof(featureId));

            if (includedQuota.HasValue && includedQuota.Value < 0)
                throw new DomainException("Plan kotası negatif olamaz.");

            PlanId = planId;
            FeatureId = featureId;
            IsEnabled = isEnabled;
            IncludedQuota = includedQuota;
        }

        public void SetEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetQuota(int? includedQuota)
        {
            if (includedQuota.HasValue && includedQuota.Value < 0)
                throw new DomainException("Plan kotası negatif olamaz.");

            IncludedQuota = includedQuota;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
