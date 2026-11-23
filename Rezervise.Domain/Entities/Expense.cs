using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İşletme gider kaydı (kıra, fatura, mal alımı vb.).
    /// Durum alani EnumSaleStatus değerlerini paylaşır
    /// (Pending = onaylanmamış, Completed = kesinleşen gider).
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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (amount <= 0)
                throw new DomainException("Gider tutarı sıfırdan büyük olmalıdır.");

            if (description is not null && description.Length > 500)
                throw new DomainException("Gider açıklaması 500 karakterden uzun olamaz.");

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
                throw new DomainException("Para birimi boş olamaz.");

            if (currency.Length != 3)
                throw new DomainException("Para birimi 3 karakter olmalıdır.");

            Currency = currency.ToUpperInvariant();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
