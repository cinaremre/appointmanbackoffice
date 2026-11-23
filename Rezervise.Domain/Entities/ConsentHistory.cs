using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İletişim izni değişikliğinin değişmez kaydı (doküman Modül 16).
    /// Append-only: kayıtlar güncellenmez/silinmez, sadece eklenir.
    /// Denetim ve hukuki ispat yükümlülüğü için tutulur.
    /// </summary>
    public class ConsentHistory : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CustomerId { get; private set; }
        public EnumConsentChannel Channel { get; private set; }
        public EnumConsentPurpose Purpose { get; private set; }
        public EnumConsentAction Action { get; private set; }

        /// <summary>Değişikliğin geldiği yer: "api", "import" vb.</summary>
        public string Source { get; private set; }

        public string? EvidenceReference { get; private set; }

        /// <summary>Oluşma anı (UTC).</summary>
        public DateTime OccurredAt { get; private set; }

        private ConsentHistory() { } // EF Core

        public ConsentHistory(
            Guid businessId,
            Guid customerId,
            EnumConsentChannel channel,
            EnumConsentPurpose purpose,
            EnumConsentAction action,
            string source,
            string? evidenceReference = null)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Müşteri ID boş olamaz.", nameof(customerId));

            if (string.IsNullOrWhiteSpace(source))
                throw new DomainException("İzin kaynağı boş olamaz.");

            if (source.Length > 50)
                throw new DomainException("İzin kaynağı 50 karakterden uzun olamaz.");

            if (evidenceReference != null && evidenceReference.Length > 200)
                throw new DomainException("İzin kanıt referansı 200 karakterden uzun olamaz.");

            BusinessId = businessId;
            CustomerId = customerId;
            Channel = channel;
            Purpose = purpose;
            Action = action;
            Source = source.Trim();
            EvidenceReference = evidenceReference;
            OccurredAt = DateTime.UtcNow;
        }
    }
}
