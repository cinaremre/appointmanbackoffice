using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Prim (komisyon) kurali. Kural bir calisana, bir hizmete/urune veya
    /// isletme geneline uygulanabilir; kapsam alanlari (EmployeeId, ServiceId,
    /// ProductId) hepsi bossa genel kuraldir. Uygulama penceresi
    /// EffectiveFrom/EffectiveTo ile sinirlidir (UTC).
    /// </summary>
    public class CommissionRule : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Bos ise kural isletme geneli/gecerli hizmet-urun kuralidir.</summary>
        public Guid? EmployeeId { get; private set; }

        /// <summary>Bos ise kural hizmet-ozel degildir.</summary>
        public Guid? ServiceId { get; private set; }

        /// <summary>Bos ise kural urun-ozel degildir.</summary>
        public Guid? ProductId { get; private set; }

        public EnumCommissionCalculationType CalculationType { get; private set; }

        /// <summary>Yuzde tipleri icin 0-1 arasi oran (orn. %20 = 0.20).</summary>
        public decimal Rate { get; private set; }

        /// <summary>FixedPerService tipi icin hizmet basina sabit tutar.</summary>
        public decimal FixedAmount { get; private set; }

        public DateTime EffectiveFrom { get; private set; }

        public DateTime? EffectiveTo { get; private set; }

        public bool IsActive { get; private set; }

        private CommissionRule() { } // EF Core icin

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
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (effectiveFrom == default)
                throw new DomainException("Kural baslangic tarihi bos olamaz.");

            if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
                throw new DomainException("Kural bitis tarihi baslangic tarihinden once olamaz.");

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
                throw new DomainException("Komisyon orani 0-1 arasinda olmalidir (orn. %20 icin 0.20).");

            if (fixedAmount < 0)
                throw new DomainException("Sabit prim tutari negatif olamaz.");

            if (fixedAmount > 99999999.99m)
                throw new DomainException("Sabit prim tutari cok yuksek.");

            if (calculationType == EnumCommissionCalculationType.FixedPerService && fixedAmount <= 0)
                throw new DomainException("Sabit primli kuralda sabit tutar sifirdan buyuk olmalidir.");

            if (calculationType != EnumCommissionCalculationType.FixedPerService && rate <= 0)
                throw new DomainException("Yuzdeli kuralda oran sifirdan buyuk olmalidir.");
        }

        /// <summary>
        /// Kural verilen UTC aninda uygulamada mi? Aktif ve pencere icinde olmali.
        /// </summary>
        public bool IsEffectiveAt(DateTime utcNow)
            => IsActive
               && !IsDeleted
               && utcNow >= EffectiveFrom
               && (EffectiveTo is null || utcNow <= EffectiveTo.Value);

        /// <summary>Yuzde tabanli hesap mi?</summary>
        public bool IsPercentBased
            => CalculationType != EnumCommissionCalculationType.FixedPerService;

        /// <summary>
        /// Verilen baz tutar uzerinden prim tutarini hesaplar.
        /// Yuzde tiplerinde BaseAmount * Rate, sabit tipte FixedAmount doner.
        /// </summary>
        public decimal Calculate(decimal baseAmount)
        {
            if (baseAmount < 0)
                throw new DomainException("Prim baz tutari negatif olamaz.");

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
