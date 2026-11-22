using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Kanal bazli tam gonderim engeli (dokuman Modul 16).
    /// Pazarlama amaciyla izin geri cekildiginde olusur; amac fark etmeksizin
    /// o kanaldan hicbir mesaj gonderilemez. Yeni izin verildiginde silinir.
    /// </summary>
    public class CommunicationSuppression : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CustomerId { get; private set; }
        public EnumConsentChannel Channel { get; private set; }

        /// <summary>Engelin gerekcesi (orn. "Consent withdrawn").</summary>
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
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Musteri ID bos olamaz.", nameof(customerId));

            if (reason != null && reason.Length > 200)
                throw new ArgumentException("Engel gerekcesi 200 karakterden uzun olamaz.", nameof(reason));

            BusinessId = businessId;
            CustomerId = customerId;
            Channel = channel;
            Reason = reason;
        }
    }
}
