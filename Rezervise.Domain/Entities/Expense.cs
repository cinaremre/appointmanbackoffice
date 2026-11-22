using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Isletme gider kaydi (kira, fatura, mal alimi vb.).
    /// Durum alani EnumSaleStatus degerlerini paylasir
    /// (Pending = onaylanmamis, Completed = kesinlesen gider).
    /// </summary>
    public class Expense : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid? CategoryId { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }
        public DateTime ExpenseDate { get; private set; }
        public string? Description { get; private set; }
        public EnumPaymentMethod PaymentMethod { get; private set; }
        public EnumSaleStatus Status { get; private set; }

        private Expense() { }

        public Expense(Guid businessId, decimal amount, DateTime expenseDate,
            EnumPaymentMethod paymentMethod, string currency = "TRY",
            Guid? categoryId = null, string? description = null,
            EnumSaleStatus status = EnumSaleStatus.Completed)
            : this(Guid.NewGuid(), businessId, amount, expenseDate, paymentMethod,
                  currency, categoryId, description, status)
        {
        }

        public Expense(Guid id, Guid businessId, decimal amount, DateTime expenseDate,
            EnumPaymentMethod paymentMethod, string currency = "TRY",
            Guid? categoryId = null, string? description = null,
            EnumSaleStatus status = EnumSaleStatus.Completed)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (amount <= 0)
                throw new DomainException("Gider tutari sifirdan buyuk olmalidir.");

            if (description is not null && description.Length > 500)
                throw new DomainException("Gider aciklamasi 500 karakterden uzun olamaz.");

            SetCurrency(currency);
            BusinessId = businessId;
            Amount = Math.Round(amount, 2);
            ExpenseDate = expenseDate;
            PaymentMethod = paymentMethod;
            CategoryId = categoryId;
            Description = description;
            Status = status;
        }

        public void SetCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new DomainException("Para birimi bos olamaz.");

            if (currency.Length != 3)
                throw new DomainException("Para birimi 3 karakter olmalidir.");

            Currency = currency.ToUpperInvariant();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
