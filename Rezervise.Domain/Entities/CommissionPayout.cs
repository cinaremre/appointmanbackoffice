using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Prim ödeme dönemi. Dönem kapatılırken dönem içindeki Accrued
    /// tahakkuklar toplanır ve ödenir; ödeme kaydı dönemi temsil eder.
    /// </summary>
    public class CommissionPayout : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        public Guid EmployeeId { get; private set; }

        /// <summary>Dönem başlangıcı (UTC, dahil).</summary>
        public DateTime PeriodStart { get; private set; }

        /// <summary>Dönem sonu (UTC, dahil).</summary>
        public DateTime PeriodEnd { get; private set; }

        /// <summary>Dönemde ödenen toplam prim tutarı.</summary>
        public decimal Amount { get; private set; }

        public EnumCommissionPayoutStatus Status { get; private set; }

        public DateTime? PaidAt { get; private set; }

        private CommissionPayout() { } // EF Core için

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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (employeeId == Guid.Empty)
                throw new ArgumentException("Çalışan ID boş olamaz.", nameof(employeeId));

            if (periodStart == default || periodEnd == default)
                throw new DomainException("Ödeme dönem tarihleri boş olamaz.");

            if (periodEnd < periodStart)
                throw new DomainException("Ödeme dönem sonu başlangıçtan önce olamaz.");

            if (amount < 0)
                throw new DomainException("Ödeme tutarı negatif olamaz.");

            if (amount > 99999999.99m)
                throw new DomainException("Ödeme tutarı çok yüksek.");

            BusinessId = businessId;
            EmployeeId = employeeId;
            PeriodStart = DateTime.SpecifyKind(periodStart, DateTimeKind.Utc);
            PeriodEnd = DateTime.SpecifyKind(periodEnd, DateTimeKind.Utc);
            Amount = Math.Round(amount, 2);
            Status = EnumCommissionPayoutStatus.Pending;
        }

        /// <summary>Dönemi ödenmiş olarak kapatır.</summary>
        public void MarkAsPaid(DateTime paidAtUtc)
        {
            if (Status == EnumCommissionPayoutStatus.Paid)
                throw new DomainException("Ödeme dönemi zaten ödenmiş.");

            Status = EnumCommissionPayoutStatus.Paid;
            PaidAt = DateTime.SpecifyKind(paidAtUtc, DateTimeKind.Utc);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
