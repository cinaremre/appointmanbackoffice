using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Bir donemdeki ozellik kullanim sayaci (dokuman Modul 14).
    /// Donem "2026-11" biciminde tutulur; upsert islemi
    /// FeatureAccessService tarafindan yapilir (domain Increment metodu yoktur).
    /// </summary>
    public class FeatureUsage : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid FeatureId { get; private set; }

        /// <summary>Kullanim donemi, "YYYY-MM" biciminde (orn. "2026-11").</summary>
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
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (featureId == Guid.Empty)
                throw new ArgumentException("Ozellik ID bos olamaz.", nameof(featureId));

            if (string.IsNullOrWhiteSpace(usagePeriod))
                throw new DomainException("Kullanim donemi bos olamaz.");

            usagePeriod = usagePeriod.Trim();

            if (usagePeriod.Length != 7 || usagePeriod[4] != '-')
                throw new DomainException("Kullanim donemi YYYY-MM biciminde olmali.");

            if (usedQuantity < 0)
                throw new DomainException("Kullanim miktari negatif olamaz.");

            BusinessId = businessId;
            FeatureId = featureId;
            UsagePeriod = usagePeriod;
            UsedQuantity = usedQuantity;
        }

        /// <summary>
        /// Upsert adiminda mutlak degeri atar; arttirma mantigi uygulama
        /// katmanindadir (FeatureAccessService yeni toplami hesaplar).
        /// </summary>
        public void SetUsedQuantity(int usedQuantity)
        {
            if (usedQuantity < 0)
                throw new DomainException("Kullanim miktari negatif olamaz.");

            UsedQuantity = usedQuantity;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
