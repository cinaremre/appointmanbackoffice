using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Bekleme listesi kaydı (doküman Modül 10, yarım otomatik mod).
    /// Müşteri hangi hizmeti hangi tarih aralığında istediğini bildirir;
    /// işletme uygun slot açıldığında elle teklif üretir.
    /// </summary>
    public class WaitlistEntry : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CustomerId { get; private set; }
        public Guid ServiceId { get; private set; }

        /// <summary>Müşterinin tercih ettiği çalışan; ilk sürümde eşleştirmede yok sayılır.</summary>
        public Guid? PreferredEmployeeId { get; private set; }

        public DateTime EarliestDate { get; private set; }
        public DateTime? LatestDate { get; private set; }

        /// <summary>Tercih edilen gün içi saat aralığı ("09:00-17:00" gibi). Opsiyonel.</summary>
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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Müşteri ID boş olamaz.", nameof(customerId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID boş olamaz.", nameof(serviceId));

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

        /// <summary>Kaydı müşterinin kendisi veya işletme iptal eder.</summary>
        public void Cancel(DateTime? now = null)
        {
            if (Status != EnumWaitlistEntryStatus.Active)
                throw new DomainException("Waitlist.EntryNotActive");

            Status = EnumWaitlistEntryStatus.Cancelled;
            UpdatedAt = now ?? DateTime.UtcNow;
        }

        /// <summary>Müşteri bir teklifi kabul etti; kayıt tamamlanır.</summary>
        public void Fulfill(DateTime? now = null)
        {
            if (Status != EnumWaitlistEntryStatus.Active)
                throw new DomainException("Waitlist.EntryNotActive");

            Status = EnumWaitlistEntryStatus.Fulfilled;
            UpdatedAt = now ?? DateTime.UtcNow;
        }

        /// <summary>Kaydın geçerlilik süresi doldu.</summary>
        public void Expire(DateTime? now = null)
        {
            if (Status != EnumWaitlistEntryStatus.Active)
                return;

            Status = EnumWaitlistEntryStatus.Expired;
            UpdatedAt = now ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Verilen slot zamanı kaydın tarih koşullarına uyuyor mü?
        /// Slot, EarliestDate ile (varsa) LatestDate arasında olmalı.
        /// </summary>
        public bool MatchesSlot(DateTime slotStart)
        {
            if (slotStart < EarliestDate)
                return false;

            if (LatestDate.HasValue && slotStart > LatestDate.Value)
                return false;

            return true;
        }

        /// <summary>"HH:mm-HH:mm" biçimini doğrular; boş değer kabul edilir.</summary>
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
