using AppointMan.Domain.Enums;
using AppointMan.Domain.Events.Consent;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Musteri x kanal x amac bazinda guncel iletisim izni (dokuman Modul 16).
    /// Tek kayit per (BusinessId, CustomerId, Channel, Purpose); handler upsert yapar.
    /// Gecmis ConsentHistory kayitlarindan izlenir.
    /// </summary>
    public class CustomerCommunicationPreference : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CustomerId { get; private set; }
        public EnumConsentChannel Channel { get; private set; }
        public EnumConsentPurpose Purpose { get; private set; }

        /// <summary>KVKK/acik riza gerekcesi (orn. "consent_form", "legitimate_interest").</summary>
        public string? LegalBasis { get; private set; }

        /// <summary>Izni ispatlayan referans (form ID, import kayit ID vb.).</summary>
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
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Musteri ID bos olamaz.", nameof(customerId));

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
        /// Izni gunceller. Mevcut deger ayniysa hicbir degisiklik ve event uretilmez.
        /// Degisirse CommunicationConsentChangedEvent eklenir.
        /// </summary>
        /// <returns>Degisiklik olduysa true; deger ayniysa false.</returns>
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
