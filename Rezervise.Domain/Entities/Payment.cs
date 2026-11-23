using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Ödeme kaydı. İadeler negatif tutarlı kayıt yerine ayrı bir ters
    /// kayıt olarak tutulur (ters kayıt ilkesi): iade kaydında IsRefund=true
    /// olur ve RefersToPaymentId ile orijinal ödemeye bağlanır.
    /// Durum alani EnumSaleStatus değerlerini paylaşır.
    /// </summary>
    public class Payment : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid SaleId { get; private set; }
        public Guid? CashSessionId { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }
        public EnumPaymentMethod PaymentMethod { get; private set; }
        public DateTime PaidAt { get; private set; }
        public EnumSaleStatus Status { get; private set; }
        public string? ExternalTransactionId { get; private set; }

        /// <summary>Bu kayıt bir iade (ters) kaydı mi?</summary>
        public bool IsRefund { get; private set; }

        /// <summary>Ters kayıt ise bağlı öldüğü orijinal ödeme ID.</summary>
        public Guid? RefersToPaymentId { get; private set; }

        private Payment() { }

        public Payment(Guid businessId, Guid saleId, decimal amount,
            EnumPaymentMethod paymentMethod, DateTime paidAt,
            Guid? cashSessionId = null, string currency = "TRY",
            string? externalTransactionId = null)
            : this(Guid.NewGuid(), businessId, saleId, amount, paymentMethod, paidAt,
                  cashSessionId, currency, externalTransactionId)
        {
        }

        public Payment(Guid id, Guid businessId, Guid saleId, decimal amount,
            EnumPaymentMethod paymentMethod, DateTime paidAt,
            Guid? cashSessionId = null, string currency = "TRY",
            string? externalTransactionId = null)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (saleId == Guid.Empty)
                throw new ArgumentException("Satış ID boş olamaz.", nameof(saleId));

            if (amount <= 0)
                throw new DomainException("Ödeme tutarı sıfırdan büyük olmalıdır.");

            if (externalTransactionId is not null && externalTransactionId.Length > 100)
                throw new DomainException("Dış işlem kimliği 100 karakterden uzun olamaz.");

            SetCurrency(currency);
            BusinessId = businessId;
            SaleId = saleId;
            CashSessionId = cashSessionId;
            Amount = Math.Round(amount, 2);
            PaymentMethod = paymentMethod;
            PaidAt = paidAt;
            Status = EnumSaleStatus.Completed;
            ExternalTransactionId = externalTransactionId;
        }

        /// <summary>
        /// Orijinal ödemeye bağlı bir iade (ters) kaydı oluşturur.
        /// Tutar pozitif saklanır; yönü IsRefund bayrağı ile anlaşılır.
        /// </summary>
        /// <param name="original">İade edilen orijinal ödeme.</param>
        /// <param name="amount">İade tutarı (pozitif).</param>
        /// <param name="paidAt">İşlem zamanı (UTC).</param>
        /// <param name="cashSessionId">Nakit iadelerde açık kasa oturumu.</param>
        public static Payment CreateRefund(Payment original, decimal amount,
            DateTime paidAt, Guid? cashSessionId = null)
        {
            if (original is null)
                throw new ArgumentNullException(nameof(original));

            if (original.IsRefund)
                throw new DomainException("İade kaydı tekrar iade edilemez.");

            if (amount <= 0)
                throw new DomainException("İade tutarı sıfırdan büyük olmalıdır.");

            if (amount > original.Amount)
                throw new DomainException("İade tutarı orijinal ödemeyi geçemez.");

            return new Payment(
                original.BusinessId,
                original.SaleId,
                amount,
                original.PaymentMethod,
                paidAt,
                cashSessionId,
                original.Currency)
            {
                IsRefund = true,
                RefersToPaymentId = original.Id
            };
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

        /// <summary>
        /// Ödemeyi tamamen iade edilmiş olarak işaretler.
        /// Yalnızca tamamlanmış ödemeler işaretlenebilir.
        /// </summary>
        public void MarkRefunded(DateTime? now = null)
        {
            if (Status != EnumSaleStatus.Completed)
                return;

            Status = EnumSaleStatus.Refunded;
            UpdatedAt = now ?? DateTime.UtcNow;
        }
    }
}
