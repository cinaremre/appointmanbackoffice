using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Bekleme listesi kaydına gönderilen slot teklifi (doküman Modül 10).
    /// İşletme onay adımında üretilir; müşteri kabul/reddeder ya da süre dolar.
    /// </summary>
    public class WaitlistOffer : BaseEntity
    {
        public Guid WaitlistEntryId { get; private set; }
        public Guid BusinessId { get; private set; }

        /// <summary>Açılan slotun başlangıç zamanı (randevu saati, UTC).</summary>
        public DateTime SlotStart { get; private set; }

        /// <summary>Teklifin geçerlilik bitişi; <see cref="Domain.Common.WaitlistPolicy.OfferLifetime"/> ile hesaplanır.</summary>
        public DateTime ExpiresAt { get; private set; }

        public EnumWaitlistOfferStatus Status { get; private set; }
        public DateTime? AcceptedAt { get; private set; }

        // Navigation
        public WaitlistEntry WaitlistEntry { get; private set; }

        private WaitlistOffer() { } // EF Core

        public WaitlistOffer(
            Guid waitlistEntryId,
            Guid businessId,
            DateTime slotStart,
            DateTime expiresAt,
            DateTime now)
            : base(Guid.NewGuid())
        {
            if (waitlistEntryId == Guid.Empty)
                throw new ArgumentException("Bekleme listesi kaydı ID boş olamaz.", nameof(waitlistEntryId));

            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (slotStart == default)
                throw new DomainException("Waitlist.SlotStartRequired");

            WaitlistEntryId = waitlistEntryId;
            BusinessId = businessId;
            SlotStart = slotStart;
            ExpiresAt = expiresAt;
            Status = EnumWaitlistOfferStatus.Pending;
            CreatedAt = now;
        }

        /// <summary>
        /// Teklifi kabul eder. Yalnızca Pending ve süresi henüz dolmamış
        /// teklifler kabul edilebilir; aksi halde DomainException.
        /// </summary>
        /// <param name="now">Referans UTC zaman.</param>
        public void Accept(DateTime now)
        {
            if (Status != EnumWaitlistOfferStatus.Pending)
                throw new DomainException("Waitlist.OfferNotPending");

            if (ExpiresAt < now)
                throw new DomainException("Waitlist.OfferExpired");

            Status = EnumWaitlistOfferStatus.Accepted;
            AcceptedAt = now;
            UpdatedAt = now;
        }

        /// <summary>Süresi dolan Pending teklifi Expired işaretler.</summary>
        public void Expire(DateTime now)
        {
            if (Status != EnumWaitlistOfferStatus.Pending)
                return;

            Status = EnumWaitlistOfferStatus.Expired;
            UpdatedAt = now;
        }

        /// <summary>
        /// Aynı slot için başka bir teklif kabul edildiğinde kalan Pending
        /// teklifler Superseded yapılır.
        /// </summary>
        public void Supersede()
        {
            if (Status != EnumWaitlistOfferStatus.Pending)
                return;

            Status = EnumWaitlistOfferStatus.Superseded;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
