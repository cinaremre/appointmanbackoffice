using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Bekleme listesi kaydina gonderilen slot teklifi (dokuman Modul 10).
    /// Isletme onay adiminda uretilir; musteri kabul/reddeder ya da sure dolar.
    /// </summary>
    public class WaitlistOffer : BaseEntity
    {
        public Guid WaitlistEntryId { get; private set; }
        public Guid BusinessId { get; private set; }

        /// <summary>Acilan slotun baslangic zamani (randevu saati, UTC).</summary>
        public DateTime SlotStart { get; private set; }

        /// <summary>Teklifin gecerlilik bitisi; <see cref="Domain.Common.WaitlistPolicy.OfferLifetime"/> ile hesaplanir.</summary>
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
                throw new ArgumentException("Bekleme listesi kaydi ID bos olamaz.", nameof(waitlistEntryId));

            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

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
        /// Teklifi kabul eder. Yalnizca Pending ve suresi henuz dolmamis
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

        /// <summary>Suresi dolan Pending teklifi Expired isaretler.</summary>
        public void Expire(DateTime now)
        {
            if (Status != EnumWaitlistOfferStatus.Pending)
                return;

            Status = EnumWaitlistOfferStatus.Expired;
            UpdatedAt = now;
        }

        /// <summary>
        /// Ayni slot icin baska bir teklif kabul edildiginde kalan Pending
        /// teklifler Superseded yapilir.
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
