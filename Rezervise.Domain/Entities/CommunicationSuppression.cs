using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Kanal bazlı tam gönderim engeli (doküman Modül 16).
    /// Pazarlama amacıyla izin geri çekildiğinde oluşur; amaç fark etmeksizin
    /// o kanaldan hiçbir mesaj gönderilemez. Yeni izin verildiğinde silinir.
    /// </summary>
    public class CommunicationSuppression : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CustomerId { get; private set; }
        public EnumConsentChannel Channel { get; private set; }

        /// <summary>Engelin gerekçesi (orn. "Consent withdrawn").</summary>
        public string? Reason { get; private set; }

        // Navigation Properties
        public Business Business { get; private set; }
        public User Customer { get; private set; }

        private CommunicationSuppression() { } // EF Core

        public CommunicationSuppression(
            Guid businessId,
            Guid customerId,
            EnumConsentChannel channel,
            string? reason = null)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Müşteri ID boş olamaz.", nameof(customerId));

            if (reason != null && reason.Length > 200)
                throw new ArgumentException("Engel gerekçesi 200 karakterden uzun olamaz.", nameof(reason));

            BusinessId = businessId;
            CustomerId = customerId;
            Channel = channel;
            Reason = reason;
        }
    }
}
