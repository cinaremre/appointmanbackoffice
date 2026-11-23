using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Prim tahakkuku. Tahsilat tabanlı varsayımla çalışır: BaseAmount
    /// çağrı tarafından tahsil edilen tutar olarak verilir ve prim o tutar
    /// üzerinden hesaplanır. Tahakkuk defteri kaydı gibidir; durumlar
    /// Accrued -> Paid veya Accrued -> Reversed yönünde değişebilir,
    /// Paid durumundan geri alınamaz.
    /// </summary>
    public class CommissionAccrual : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        public Guid EmployeeId { get; private set; }

        /// <summary>Varsa bu tahakkuğu doğuran satış satırı.</summary>
        public Guid? SaleLineId { get; private set; }

        /// <summary>Hesaplamada kullanılan kural.</summary>
        public Guid RuleId { get; private set; }

        /// <summary>Primin hesaplandığı tahsil edilen tutar.</summary>
        public decimal BaseAmount { get; private set; }

        public decimal CommissionAmount { get; private set; }

        public EnumCommissionAccrualStatus Status { get; private set; }

        public DateTime CalculatedAt { get; private set; }

        private CommissionAccrual() { } // EF Core için

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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (employeeId == Guid.Empty)
                throw new ArgumentException("Çalışan ID boş olamaz.", nameof(employeeId));

            if (ruleId == Guid.Empty)
                throw new ArgumentException("Kural ID boş olamaz.", nameof(ruleId));

            if (baseAmount < 0)
                throw new DomainException("Prim baz tutarı negatif olamaz.");

            if (baseAmount > 99999999.99m)
                throw new DomainException("Prim baz tutarı çok yüksek.");

            if (commissionAmount < 0)
                throw new DomainException("Prim tutarı negatif olamaz.");

            if (commissionAmount > 99999999.99m)
                throw new DomainException("Prim tutarı çok yüksek.");

            if (calculatedAt == default)
                throw new DomainException("Hesaplama tarihi boş olamaz.");

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
        /// Tahakkuku ters çevirir. Yalnızca Accrued durumdaki tahakkuklar
        /// ters çevrilebilir; ödenmiş (Paid) veya zaten ters çevrilmiş
        /// kayıtlar reddedilir.
        /// </summary>
        public void Reverse()
        {
            if (Status == EnumCommissionAccrualStatus.Paid)
                throw new DomainException("Ödenmiş prim tahakkuku ters çevrilemez.");

            if (Status == EnumCommissionAccrualStatus.Reversed)
                throw new DomainException("Prim tahakkuku zaten ters çevrilmiş.");

            Status = EnumCommissionAccrualStatus.Reversed;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Tahakkuku ödenmiş olarak işaretler (ödeme dönemi kapatılırken).
        /// Yalnızca Accrued durumdaki tahakkuklar ödenebilir.
        /// </summary>
        public void MarkAsPaid()
        {
            if (Status == EnumCommissionAccrualStatus.Paid)
                throw new DomainException("Prim tahakkuku zaten ödenmiş.");

            if (Status == EnumCommissionAccrualStatus.Reversed)
                throw new DomainException("Ters çevrilmiş tahakkuk ödenmez.");

            Status = EnumCommissionAccrualStatus.Paid;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
