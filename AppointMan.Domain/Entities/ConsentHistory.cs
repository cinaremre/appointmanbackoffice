using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Iletisim izni degisikliginin degismez kaydi (dokuman Modul 16).
    /// Append-only: kayitlar guncellenmez/silinmez, sadece eklenir.
    /// Denetim ve hukuki ispat yukumlulugu icin tutulur.
    /// </summary>
    public class ConsentHistory : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CustomerId { get; private set; }
        public EnumConsentChannel Channel { get; private set; }
        public EnumConsentPurpose Purpose { get; private set; }
        public EnumConsentAction Action { get; private set; }

        /// <summary>Degisikligin geldigi yer: "api", "import" vb.</summary>
        public string Source { get; private set; }

        public string? EvidenceReference { get; private set; }

        /// <summary>Olusma ani (UTC).</summary>
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
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Musteri ID bos olamaz.", nameof(customerId));

            if (string.IsNullOrWhiteSpace(source))
                throw new DomainException("Izin kaynagi bos olamaz.");

            if (source.Length > 50)
                throw new DomainException("Izin kaynagi 50 karakterden uzun olamaz.");

            if (evidenceReference != null && evidenceReference.Length > 200)
                throw new DomainException("Izin kanit referansi 200 karakterden uzun olamaz.");

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
