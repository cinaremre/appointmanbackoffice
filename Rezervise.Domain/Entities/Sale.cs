using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// SatisAggregate kokudur. Satırlar (SaleLine) ve ödemeler (Payment)
    /// bu aggregate'in alt kayıtlarıdır.
    /// Toplamlar satır ekleme ve sipariş indirimi sırasında yeniden
    /// hesaplanır: GrandTotal = Subtotal - DiscountTotal + TaxTotal.
    /// </summary>
    public class Sale : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid? AppointmentId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public DateTime SoldAt { get; private set; }

        public decimal Subtotal { get; private set; }
        public decimal DiscountTotal { get; private set; }
        public decimal TaxTotal { get; private set; }
        public decimal GrandTotal { get; private set; }
        public string Currency { get; private set; }

        public EnumSaleStatus Status { get; private set; }

        private readonly List<SaleLine> _lines = new();
        public IReadOnlyCollection<SaleLine> Lines => _lines.AsReadOnly();

        /// <summary>Sipariş seviyesi indirim; satır indirimlerinden ayrı tutulur.</summary>
        private decimal _orderDiscount;

        private Sale() { }

        public Sale(Guid businessId, DateTime soldAt,
            string currency = "TRY", Guid? appointmentId = null, Guid? customerId = null)
            : this(Guid.NewGuid(), businessId, soldAt, currency, appointmentId, customerId)
        {
        }

        public Sale(Guid id, Guid businessId, DateTime soldAt,
            string currency = "TRY", Guid? appointmentId = null, Guid? customerId = null)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            SetCurrency(currency);
            BusinessId = businessId;
            SoldAt = soldAt;
            AppointmentId = appointmentId;
            CustomerId = customerId;
            Status = EnumSaleStatus.Pending;
        }

        /// <summary>
        /// Geçerli durum geçilerinin tek kaynağı. Tamamlanmış ve iptal
        /// edilmiş satışlar terminaldur; hiçbir geçişe izin verilmez.
        /// </summary>
        public static bool IsValidStatusTransition(EnumSaleStatus from, EnumSaleStatus to)
        {
            return (from, to) switch
            {
                (EnumSaleStatus.Pending, EnumSaleStatus.Completed) => true,
                (EnumSaleStatus.Pending, EnumSaleStatus.Cancelled) => true,
                _ => false
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
        /// Satışa satır ekler ve toplamları yeniden hesaplar.
        /// Yalnızca bekleyen satışa satır eklenebilir.
        /// </summary>
        public void AddLine(SaleLine line)
        {
            if (line is null)
                throw new ArgumentNullException(nameof(line));

            if (line.SaleId != Id)
                throw new DomainException("Satış satırı başka bir satışa ait.");

            if (Status != EnumSaleStatus.Pending)
                throw new DomainException("Tamamlanmış veya iptal edilmiş satışa satır eklenemez.");

            _lines.Add(line);
            RecalculateTotals();
        }

        /// <summary>
        /// Sipariş seviyesi indirim uygular ve toplamları yeniden hesaplar.
        /// </summary>
        public void ApplyOrderDiscount(decimal discountTotal)
        {
            if (discountTotal < 0)
                throw new DomainException("Sipariş indirimi negatif olamaz.");

            _orderDiscount = Math.Round(discountTotal, 2);
            RecalculateTotals();
        }

        /// <summary>Satışı tamamlar. Yalnızca bekleyen satış tamamlanabilir.</summary>
        public void Complete(DateTime? now = null)
        {
            if (!IsValidStatusTransition(Status, EnumSaleStatus.Completed))
                throw new DomainException("Bekleyen olmayan satış tamamlanamaz.");

            var currentTime = now ?? DateTime.UtcNow;
            Status = EnumSaleStatus.Completed;
            UpdatedAt = currentTime;
        }

        /// <summary>
        /// Satışı iptal eder; iptal için yeni kayıt üretilmez, yalnızca
        /// durum değişir. Yalnızca bekleyen satış iptal edilebilir.
        /// </summary>
        public void Cancel(DateTime? now = null)
        {
            if (!IsValidStatusTransition(Status, EnumSaleStatus.Cancelled))
                throw new DomainException("Bekleyen olmayan satış iptal edilemez.");

            var currentTime = now ?? DateTime.UtcNow;
            Status = EnumSaleStatus.Cancelled;
            UpdatedAt = currentTime;
        }

        private void RecalculateTotals()
        {
            Subtotal = Math.Round(_lines.Sum(l => l.Quantity * l.UnitPrice), 2);
            DiscountTotal = Math.Round(_lines.Sum(l => l.DiscountAmount) + _orderDiscount, 2);
            TaxTotal = Math.Round(_lines.Sum(l => l.TaxAmount), 2);
            GrandTotal = Math.Round(Subtotal - DiscountTotal + TaxTotal, 2);

            if (GrandTotal < 0)
                throw new DomainException("Satış toplamı negatif olamaz.");

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
