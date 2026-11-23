using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Bir dönemdeki özellik kullanım sayacı (doküman Modül 14).
    /// Dönem "2026-11" biçiminde tutulur; upsert işlemi
    /// FeatureAccessService tarafından yapılır (domain Increment metodu yoktur).
    /// </summary>
    public class FeatureUsage : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid FeatureId { get; private set; }

        /// <summary>Kullanım dönemi, "YYYY-MM" biçiminde (orn. "2026-11").</summary>
        public string UsagePeriod { get; private set; }

        public int UsedQuantity { get; private set; }

        // Navigation Properties
        public Business Business { get; private set; }
        public FeatureDefinition Feature { get; private set; }

        private FeatureUsage() { } // EF Core

        public FeatureUsage(Guid businessId, Guid featureId, string usagePeriod, int usedQuantity = 0)
            : this(Guid.NewGuid(), businessId, featureId, usagePeriod, usedQuantity)
        {
        }

        public FeatureUsage(Guid id, Guid businessId, Guid featureId, string usagePeriod, int usedQuantity = 0)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (featureId == Guid.Empty)
                throw new ArgumentException("Özellik ID boş olamaz.", nameof(featureId));

            if (string.IsNullOrWhiteSpace(usagePeriod))
                throw new DomainException("Kullanım dönemi boş olamaz.");

            usagePeriod = usagePeriod.Trim();

            if (usagePeriod.Length != 7 || usagePeriod[4] != '-')
                throw new DomainException("Kullanım dönemi YYYY-MM biçiminde olmalı.");

            if (usedQuantity < 0)
                throw new DomainException("Kullanım miktarı negatif olamaz.");

            BusinessId = businessId;
            FeatureId = featureId;
            UsagePeriod = usagePeriod;
            UsedQuantity = usedQuantity;
        }

        /// <summary>
        /// Upsert adımında mutlak değeri atar; arttırma mantığı uygulama
        /// katmanındadır (FeatureAccessService yeni toplamı hesaplar).
        /// </summary>
        public void SetUsedQuantity(int usedQuantity)
        {
            if (usedQuantity < 0)
                throw new DomainException("Kullanım miktarı negatif olamaz.");

            UsedQuantity = usedQuantity;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
