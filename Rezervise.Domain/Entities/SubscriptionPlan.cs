using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Abonelik planı (doküman Modül 14). Hangi özelliklerin hangi kota ile
    /// plana dahil öldüğü PlanFeature kayıtları üzerinden belirlenir.
    /// </summary>
    public class SubscriptionPlan : BaseEntity
    {
        public string Name { get; private set; }
        public decimal Price { get; private set; }

        /// <summary>ISO para birimi kodu (orn. "TRY").</summary>
        public string Currency { get; private set; }

        public EnumBillingPeriod BillingPeriod { get; private set; }
        public bool IsActive { get; private set; }

        private SubscriptionPlan() { } // EF Core

        public SubscriptionPlan(string name, decimal price, string currency, EnumBillingPeriod billingPeriod)
            : this(Guid.NewGuid(), name, price, currency, billingPeriod)
        {
        }

        public SubscriptionPlan(Guid id, string name, decimal price, string currency, EnumBillingPeriod billingPeriod)
            : base(id)
        {
            SetName(name);
            SetPrice(price);
            SetCurrency(currency);
            BillingPeriod = billingPeriod;
            IsActive = true;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Plan adı boş olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Plan adı 2-100 karakter arasında olmalı.");

            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPrice(decimal price)
        {
            if (price < 0)
                throw new DomainException("Plan fiyatı negatif olamaz.");

            Price = price;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new DomainException("Para birimi boş olamaz.");

            currency = currency.Trim().ToUpperInvariant();

            if (currency.Length != 3)
                throw new DomainException("Para birimi 3 harfli ISO kodu olmalı (orn. TRY).");

            Currency = currency;
            UpdatedAt = DateTime.UtcNow;
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
