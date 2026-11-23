using Rezervise.Domain.Enums;
using Rezervise.Domain.Events.Consent;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Müşteri x kanal x amaç bazında güncel iletişim izni (doküman Modül 16).
    /// Tek kayıt per (BusinessId, CustomerId, Channel, Purpose); handler upsert yapar.
    /// Geçmiş ConsentHistory kayıtlarından izlenir.
    /// </summary>
    public class CustomerCommunicationPreference : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CustomerId { get; private set; }
        public EnumConsentChannel Channel { get; private set; }
        public EnumConsentPurpose Purpose { get; private set; }

        /// <summary>KVKK/açık rıza gerekçesi (orn. "consent_form", "legitimate_interest").</summary>
        public string? LegalBasis { get; private set; }

        /// <summary>İzni ispatlayan referans (form ID, import kayıt ID vb.).</summary>
        public string? EvidenceReference { get; private set; }

        public bool IsAllowed { get; private set; }

        // Navigation Properties
        public Business Business { get; private set; }
        public User Customer { get; private set; }

        private CustomerCommunicationPreference() { } // EF Core

        public CustomerCommunicationPreference(
            Guid businessId,
            Guid customerId,
            EnumConsentChannel channel,
            EnumConsentPurpose purpose,
            bool isAllowed,
            string? legalBasis = null,
            string? evidenceReference = null)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Müşteri ID boş olamaz.", nameof(customerId));

            BusinessId = businessId;
            CustomerId = customerId;
            Channel = channel;
            Purpose = purpose;
            IsAllowed = isAllowed;
            LegalBasis = legalBasis;
            EvidenceReference = evidenceReference;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new CommunicationConsentChangedEvent(
                Id, BusinessId, CustomerId, Channel, Purpose, isAllowed));
        }

        /// <summary>
        /// İzni günceller. Mevcut değer aynıysa hiçbir değişiklik ve event üretilmez.
        /// Değişirse CommunicationConsentChangedEvent eklenir.
        /// </summary>
        /// <returns>Değişiklik olduysa true; değer aynıysa false.</returns>
        public bool Set(
            bool isAllowed,
            string? legalBasis = null,
            string? evidenceReference = null)
        {
            if (IsAllowed == isAllowed)
                return false;

            IsAllowed = isAllowed;
            LegalBasis = legalBasis;
            EvidenceReference = evidenceReference;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new CommunicationConsentChangedEvent(
                Id, BusinessId, CustomerId, Channel, Purpose, isAllowed));

            return true;
        }
    }
}
