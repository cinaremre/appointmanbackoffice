using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Odeme kaydi. Iadeler negatif tutarli kayit yerine ayri bir ters
    /// kayit olarak tutulur (ters kayit ilkesi): iade kaydinda IsRefund=true
    /// olur ve RefersToPaymentId ile orijinal odemeye baglanir.
    /// Durum alani EnumSaleStatus degerlerini paylasir.
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

        /// <summary>Bu kayit bir iade (ters) kaydi mi?</summary>
        public bool IsRefund { get; private set; }

        /// <summary>Ters kayit ise bagli oldugu orijinal odeme ID.</summary>
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
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (saleId == Guid.Empty)
                throw new ArgumentException("Satis ID bos olamaz.", nameof(saleId));

            if (amount <= 0)
                throw new DomainException("Odeme tutari sifirdan buyuk olmalidir.");

            if (externalTransactionId is not null && externalTransactionId.Length > 100)
                throw new DomainException("Dis islem kimligi 100 karakterden uzun olamaz.");

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
        /// Orijinal odemeye bagli bir iade (ters) kaydi olusturur.
        /// Tutar pozitif saklanir; yonu IsRefund bayragi ile anlasilir.
        /// </summary>
        /// <param name="original">Iade edilen orijinal odeme.</param>
        /// <param name="amount">Iade tutari (pozitif).</param>
        /// <param name="paidAt">Islem zamani (UTC).</param>
        /// <param name="cashSessionId">Nakit iadelerde acik kasa oturumu.</param>
        public static Payment CreateRefund(Payment original, decimal amount,
            DateTime paidAt, Guid? cashSessionId = null)
        {
            if (original is null)
                throw new ArgumentNullException(nameof(original));

            if (original.IsRefund)
                throw new DomainException("Iade kaydi tekrar iade edilemez.");

            if (amount <= 0)
                throw new DomainException("Iade tutari sifirdan buyuk olmalidir.");

            if (amount > original.Amount)
                throw new DomainException("Iade tutari orijinal odemeyi gecemez.");

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
                throw new DomainException("Para birimi bos olamaz.");

            if (currency.Length != 3)
                throw new DomainException("Para birimi 3 karakter olmalidir.");

            Currency = currency.ToUpperInvariant();
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Odemeyi tamamen iade edilmis olarak isaretler.
        /// Yalnizca tamamlanmis odemeler isaretlenebilir.
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
