using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Prim odeme donemi. Donem kapatilirken donem icindeki Accrued
    /// tahakkuklar toplanir ve odenir; odeme kaydi donemi temsil eder.
    /// </summary>
    public class CommissionPayout : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        public Guid EmployeeId { get; private set; }

        /// <summary>Donem baslangici (UTC, dahil).</summary>
        public DateTime PeriodStart { get; private set; }

        /// <summary>Donem sonu (UTC, dahil).</summary>
        public DateTime PeriodEnd { get; private set; }

        /// <summary>Donemde odenen toplam prim tutari.</summary>
        public decimal Amount { get; private set; }

        public EnumCommissionPayoutStatus Status { get; private set; }

        public DateTime? PaidAt { get; private set; }

        private CommissionPayout() { } // EF Core icin

        public CommissionPayout(
            Guid businessId,
            Guid employeeId,
            DateTime periodStart,
            DateTime periodEnd,
            decimal amount)
            : this(Guid.NewGuid(), businessId, employeeId, periodStart, periodEnd, amount)
        {
        }

        public CommissionPayout(
            Guid id,
            Guid businessId,
            Guid employeeId,
            DateTime periodStart,
            DateTime periodEnd,
            decimal amount)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (employeeId == Guid.Empty)
                throw new ArgumentException("Calisan ID bos olamaz.", nameof(employeeId));

            if (periodStart == default || periodEnd == default)
                throw new DomainException("Odeme donem tarihleri bos olamaz.");

            if (periodEnd < periodStart)
                throw new DomainException("Odeme donem sonu baslangictan once olamaz.");

            if (amount < 0)
                throw new DomainException("Odeme tutari negatif olamaz.");

            if (amount > 99999999.99m)
                throw new DomainException("Odeme tutari cok yuksek.");

            BusinessId = businessId;
            EmployeeId = employeeId;
            PeriodStart = DateTime.SpecifyKind(periodStart, DateTimeKind.Utc);
            PeriodEnd = DateTime.SpecifyKind(periodEnd, DateTimeKind.Utc);
            Amount = Math.Round(amount, 2);
            Status = EnumCommissionPayoutStatus.Pending;
        }

        /// <summary>Donemi odenmis olarak kapatir.</summary>
        public void MarkAsPaid(DateTime paidAtUtc)
        {
            if (Status == EnumCommissionPayoutStatus.Paid)
                throw new DomainException("Odeme donemi zaten odenmis.");

            Status = EnumCommissionPayoutStatus.Paid;
            PaidAt = DateTime.SpecifyKind(paidAtUtc, DateTimeKind.Utc);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
