using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Bekleme listesi kaydi (dokuman Modul 10, yarim otomatik mod).
    /// Musteri hangi hizmeti hangi tarih araliginda istedigini bildirir;
    /// isletme uygun slot acildiginda elle teklif uretir.
    /// </summary>
    public class WaitlistEntry : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CustomerId { get; private set; }
        public Guid ServiceId { get; private set; }

        /// <summary>Musterinin tercih ettigi calisan; ilk surumde eslestirmede yok sayilir.</summary>
        public Guid? PreferredEmployeeId { get; private set; }

        public DateTime EarliestDate { get; private set; }
        public DateTime? LatestDate { get; private set; }

        /// <summary>Tercih edilen gun ici saat araligi ("09:00-17:00" gibi). Opsiyonel.</summary>
        public string? PreferredTimeRange { get; private set; }

        public EnumWaitlistEntryStatus Status { get; private set; }

        private WaitlistEntry() { } // EF Core

        public WaitlistEntry(
            Guid businessId,
            Guid customerId,
            Guid serviceId,
            DateTime earliestDate,
            DateTime? latestDate = null,
            Guid? preferredEmployeeId = null,
            string? preferredTimeRange = null)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Musteri ID bos olamaz.", nameof(customerId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID bos olamaz.", nameof(serviceId));

            if (earliestDate == default)
                throw new DomainException("Waitlist.EarliestDateRequired");

            if (latestDate.HasValue && latestDate.Value < earliestDate)
                throw new DomainException("Waitlist.LatestBeforeEarliest");

            ValidateTimeRange(preferredTimeRange);

            BusinessId = businessId;
            CustomerId = customerId;
            ServiceId = serviceId;
            EarliestDate = earliestDate;
            LatestDate = latestDate;
            PreferredEmployeeId = preferredEmployeeId;
            PreferredTimeRange = string.IsNullOrWhiteSpace(preferredTimeRange) ? null : preferredTimeRange.Trim();
            Status = EnumWaitlistEntryStatus.Active;
        }

        /// <summary>Kaydi musterinin kendisi veya isletme iptal eder.</summary>
        public void Cancel(DateTime? now = null)
        {
            if (Status != EnumWaitlistEntryStatus.Active)
                throw new DomainException("Waitlist.EntryNotActive");

            Status = EnumWaitlistEntryStatus.Cancelled;
            UpdatedAt = now ?? DateTime.UtcNow;
        }

        /// <summary>Musteri bir teklifi kabul etti; kayit tamamlanir.</summary>
        public void Fulfill(DateTime? now = null)
        {
            if (Status != EnumWaitlistEntryStatus.Active)
                throw new DomainException("Waitlist.EntryNotActive");

            Status = EnumWaitlistEntryStatus.Fulfilled;
            UpdatedAt = now ?? DateTime.UtcNow;
        }

        /// <summary>Kaydin gecerlilik suresi doldu.</summary>
        public void Expire(DateTime? now = null)
        {
            if (Status != EnumWaitlistEntryStatus.Active)
                return;

            Status = EnumWaitlistEntryStatus.Expired;
            UpdatedAt = now ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Verilen slot zamani kaydin tarih kosullarina uyuyor mu?
        /// Slot, EarliestDate ile (varsa) LatestDate arasinda olmali.
        /// </summary>
        public bool MatchesSlot(DateTime slotStart)
        {
            if (slotStart < EarliestDate)
                return false;

            if (LatestDate.HasValue && slotStart > LatestDate.Value)
                return false;

            return true;
        }

        /// <summary>"HH:mm-HH:mm" bicimini dogrular; bos deger kabul edilir.</summary>
        private static void ValidateTimeRange(string? preferredTimeRange)
        {
            if (string.IsNullOrWhiteSpace(preferredTimeRange))
                return;

            var parts = preferredTimeRange.Trim().Split('-');
            if (parts.Length != 2 ||
                !TimeSpan.TryParse(parts[0], out var start) ||
                !TimeSpan.TryParse(parts[1], out var end) ||
                start >= end)
            {
                throw new DomainException("Waitlist.InvalidTimeRange");
            }
        }
    }
}
