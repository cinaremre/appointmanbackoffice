using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Prim tahakkuku. Tahsilat tabanli varsayimla calisir: BaseAmount
    /// cagri tarafindan tahsil edilen tutar olarak verilir ve prim o tutar
    /// uzerinden hesaplanir. Tahakkuk defteri kaydi gibidir; durumlar
    /// Accrued -> Paid veya Accrued -> Reversed yonunde degisebilir,
    /// Paid durumundan geri alinamaz.
    /// </summary>
    public class CommissionAccrual : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        public Guid EmployeeId { get; private set; }

        /// <summary>Varsa bu tahakkugu doguran satis satiri.</summary>
        public Guid? SaleLineId { get; private set; }

        /// <summary>Hesaplamada kullanilan kural.</summary>
        public Guid RuleId { get; private set; }

        /// <summary>Primin hesaplandigi tahsil edilen tutar.</summary>
        public decimal BaseAmount { get; private set; }

        public decimal CommissionAmount { get; private set; }

        public EnumCommissionAccrualStatus Status { get; private set; }

        public DateTime CalculatedAt { get; private set; }

        private CommissionAccrual() { } // EF Core icin

        public CommissionAccrual(
            Guid businessId,
            Guid employeeId,
            Guid ruleId,
            decimal baseAmount,
            decimal commissionAmount,
            DateTime calculatedAt,
            Guid? saleLineId = null)
            : this(Guid.NewGuid(), businessId, employeeId, ruleId,
                  baseAmount, commissionAmount, calculatedAt, saleLineId)
        {
        }

        public CommissionAccrual(
            Guid id,
            Guid businessId,
            Guid employeeId,
            Guid ruleId,
            decimal baseAmount,
            decimal commissionAmount,
            DateTime calculatedAt,
            Guid? saleLineId = null)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (employeeId == Guid.Empty)
                throw new ArgumentException("Calisan ID bos olamaz.", nameof(employeeId));

            if (ruleId == Guid.Empty)
                throw new ArgumentException("Kural ID bos olamaz.", nameof(ruleId));

            if (baseAmount < 0)
                throw new DomainException("Prim baz tutari negatif olamaz.");

            if (baseAmount > 99999999.99m)
                throw new DomainException("Prim baz tutari cok yuksek.");

            if (commissionAmount < 0)
                throw new DomainException("Prim tutari negatif olamaz.");

            if (commissionAmount > 99999999.99m)
                throw new DomainException("Prim tutari cok yuksek.");

            if (calculatedAt == default)
                throw new DomainException("Hesaplama tarihi bos olamaz.");

            BusinessId = businessId;
            EmployeeId = employeeId;
            RuleId = ruleId;
            BaseAmount = Math.Round(baseAmount, 2);
            CommissionAmount = Math.Round(commissionAmount, 2);
            CalculatedAt = DateTime.SpecifyKind(calculatedAt, DateTimeKind.Utc);
            SaleLineId = saleLineId == Guid.Empty ? null : saleLineId;
            Status = EnumCommissionAccrualStatus.Accrued;
        }

        /// <summary>
        /// Tahakkuku ters cevirir. Yalnizca Accrued durumdaki tahakkuklar
        /// ters cevrilebilir; odenmis (Paid) veya zaten ters cevrilmis
        /// kayitlar reddedilir.
        /// </summary>
        public void Reverse()
        {
            if (Status == EnumCommissionAccrualStatus.Paid)
                throw new DomainException("Odenmis prim tahakkuku ters cevrilemez.");

            if (Status == EnumCommissionAccrualStatus.Reversed)
                throw new DomainException("Prim tahakkuku zaten ters cevrilmis.");

            Status = EnumCommissionAccrualStatus.Reversed;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Tahakkuku odenmis olarak isaretler (odeme donemi kapatilirken).
        /// Yalnizca Accrued durumdaki tahakkuklar odenebilir.
        /// </summary>
        public void MarkAsPaid()
        {
            if (Status == EnumCommissionAccrualStatus.Paid)
                throw new DomainException("Prim tahakkuku zaten odenmis.");

            if (Status == EnumCommissionAccrualStatus.Reversed)
                throw new DomainException("Ters cevrilmis tahakkuk odenmez.");

            Status = EnumCommissionAccrualStatus.Paid;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
