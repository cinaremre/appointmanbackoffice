using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Prim (komisyon) kuralı. Kural bir çalışana, bir hizmete/ürüne veya
    /// işletme geneline uygulanabilir; kapsam alanları (EmployeeId, ServiceId,
    /// ProductId) hepsi bossa genel kuraldır. Uygulama penceresi
    /// EffectiveFrom/EffectiveTo ile sınırlıdır (UTC).
    /// </summary>
    public class CommissionRule : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Boş ise kural işletme geneli/geçerli hizmet-ürün kuralıdır.</summary>
        public Guid? EmployeeId { get; private set; }

        /// <summary>Boş ise kural hizmet-özel değildir.</summary>
        public Guid? ServiceId { get; private set; }

        /// <summary>Boş ise kural ürün-özel değildir.</summary>
        public Guid? ProductId { get; private set; }

        public EnumCommissionCalculationType CalculationType { get; private set; }

        /// <summary>Yüzde tipleri için 0-1 arası oran (orn. %20 = 0.20).</summary>
        public decimal Rate { get; private set; }

        /// <summary>FixedPerService tipi için hizmet basına sabit tutar.</summary>
        public decimal FixedAmount { get; private set; }

        public DateTime EffectiveFrom { get; private set; }

        public DateTime? EffectiveTo { get; private set; }

        public bool IsActive { get; private set; }

        private CommissionRule() { } // EF Core için

        public CommissionRule(
            Guid businessId,
            EnumCommissionCalculationType calculationType,
            decimal rate,
            decimal fixedAmount,
            DateTime effectiveFrom,
            DateTime? effectiveTo = null,
            Guid? employeeId = null,
            Guid? serviceId = null,
            Guid? productId = null)
            : this(Guid.NewGuid(), businessId, calculationType, rate, fixedAmount,
                  effectiveFrom, effectiveTo, employeeId, serviceId, productId)
        {
        }

        public CommissionRule(
            Guid id,
            Guid businessId,
            EnumCommissionCalculationType calculationType,
            decimal rate,
            decimal fixedAmount,
            DateTime effectiveFrom,
            DateTime? effectiveTo = null,
            Guid? employeeId = null,
            Guid? serviceId = null,
            Guid? productId = null)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (effectiveFrom == default)
                throw new DomainException("Kural başlangıç tarihi boş olamaz.");

            if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
                throw new DomainException("Kural bitiş tarihi başlangıç tarihinden önce olamaz.");

            ValidateAmounts(calculationType, rate, fixedAmount);

            BusinessId = businessId;
            CalculationType = calculationType;
            Rate = rate;
            FixedAmount = Math.Round(fixedAmount, 2);
            EffectiveFrom = DateTime.SpecifyKind(effectiveFrom, DateTimeKind.Utc);
            EffectiveTo = effectiveTo.HasValue
                ? DateTime.SpecifyKind(effectiveTo.Value, DateTimeKind.Utc)
                : null;
            EmployeeId = employeeId == Guid.Empty ? null : employeeId;
            ServiceId = serviceId == Guid.Empty ? null : serviceId;
            ProductId = productId == Guid.Empty ? null : productId;
            IsActive = true;
        }

        private static void ValidateAmounts(
            EnumCommissionCalculationType calculationType,
            decimal rate,
            decimal fixedAmount)
        {
            if (rate < 0 || rate > 1)
                throw new DomainException("Komisyon oranı 0-1 arasında olmalıdır (orn. %20 için 0.20).");

            if (fixedAmount < 0)
                throw new DomainException("Sabit prim tutarı negatif olamaz.");

            if (fixedAmount > 99999999.99m)
                throw new DomainException("Sabit prim tutarı çok yüksek.");

            if (calculationType == EnumCommissionCalculationType.FixedPerService && fixedAmount <= 0)
                throw new DomainException("Sabit primli kuralda sabit tutar sıfırdan büyük olmalıdır.");

            if (calculationType != EnumCommissionCalculationType.FixedPerService && rate <= 0)
                throw new DomainException("Yüzdeli kuralda oran sıfırdan büyük olmalıdır.");
        }

        /// <summary>
        /// Kural verilen UTC anında uygulamada mi? Aktif ve pencere içinde olmalı.
        /// </summary>
        public bool IsEffectiveAt(DateTime utcNow)
            => IsActive
               && !IsDeleted
               && utcNow >= EffectiveFrom
               && (EffectiveTo is null || utcNow <= EffectiveTo.Value);

        /// <summary>Yüzde tabanlı hesap mi?</summary>
        public bool IsPercentBased
            => CalculationType != EnumCommissionCalculationType.FixedPerService;

        /// <summary>
        /// Verilen baz tutar üzerinden prim tutarını hesaplar.
        /// Yüzde tiplerinde BaseAmount * Rate, sabit tipte FixedAmount döner.
        /// </summary>
        public decimal Calculate(decimal baseAmount)
        {
            if (baseAmount < 0)
                throw new DomainException("Prim baz tutarı negatif olamaz.");

            if (IsPercentBased)
                return Math.Round(baseAmount * Rate, 2);

            return FixedAmount;
        }

        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
