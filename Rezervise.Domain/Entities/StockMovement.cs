using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Stok hareketi. Hareketler defter (ledger) mantığıyla çalışır:
    /// bir kez yazılır, güncellenmez veya silinmez (Update metodu yoktur).
    /// Giriş tipleri (PurchaseIn, CustomerReturn) pozitif, çıkış tipleri
    /// negatif işaretle saklanır; böylece bakiye = hareketlerin toplamı.
    /// Miktar çağrı tarafından her zaman pozitif verilir, işareti
    /// constructor hareket tipinden belirler.
    /// </summary>
    public class StockMovement : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        public Guid ProductId { get; private set; }

        public EnumStockMovementType MovementType { get; private set; }

        /// <summary>
        /// İşaretli miktar: giriş hareketlerinde pozitif, çıkış
        /// hareketlerinde negatif. Bakiye hesabı doğrudan toplanır.
        /// </summary>
        public decimal Quantity { get; private set; }

        /// <summary>Birim maliyet (girişler için alış fiyatı, çıkışlar için 0 olabilir).</summary>
        public decimal UnitCost { get; private set; }

        /// <summary>Referans kayıt tipi (orn. "Appointment", "Sale"). Çift işleme engeli için kullanılır.</summary>
        public string? ReferenceType { get; private set; }

        /// <summary>Referans kayıt ID'si. Aynı (ReferenceType, ReferenceId) ikinci kez işlenemez.</summary>
        public Guid? ReferenceId { get; private set; }

        /// <summary>Hareketin gerçekleştiği an (UTC).</summary>
        public DateTime OccurredAt { get; private set; }

        private StockMovement() { } // EF Core için

        public StockMovement(
            Guid businessId,
            Guid productId,
            EnumStockMovementType movementType,
            decimal quantity,
            decimal unitCost,
            DateTime occurredAt,
            string? referenceType = null,
            Guid? referenceId = null)
            : this(Guid.NewGuid(), businessId, productId, movementType, quantity,
                  unitCost, occurredAt, referenceType, referenceId)
        {
        }

        public StockMovement(
            Guid id,
            Guid businessId,
            Guid productId,
            EnumStockMovementType movementType,
            decimal quantity,
            decimal unitCost,
            DateTime occurredAt,
            string? referenceType = null,
            Guid? referenceId = null)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (productId == Guid.Empty)
                throw new ArgumentException("Ürün ID boş olamaz.", nameof(productId));

            // Miktar her zaman pozitif girilir; işareti hareket tipi belirler.
            if (quantity <= 0)
                throw new DomainException("Stok hareket miktarı sıfırdan büyük olmalıdır.");

            if (quantity > 99999999.99m)
                throw new DomainException("Stok hareket miktarı çok yüksek.");

            if (unitCost < 0)
                throw new DomainException("Birim maliyet negatif olamaz.");

            if (unitCost > 99999999.99m)
                throw new DomainException("Birim maliyet çok yüksek.");

            if (occurredAt == default)
                throw new DomainException("Hareket tarihi boş olamaz.");

            if (referenceId.HasValue && string.IsNullOrWhiteSpace(referenceType))
                throw new DomainException("Referans ID verildiyse referans tipi de verilmelidir.");

            BusinessId = businessId;
            ProductId = productId;
            MovementType = movementType;
            Quantity = IsIncoming(movementType) ? quantity : -quantity;
            UnitCost = Math.Round(unitCost, 2);
            OccurredAt = DateTime.SpecifyKind(occurredAt, DateTimeKind.Utc);
            ReferenceType = string.IsNullOrWhiteSpace(referenceType) ? null : referenceType.Trim();
            ReferenceId = referenceId == Guid.Empty ? null : referenceId;
        }

        /// <summary>
        /// Giriş tipi mi? Yalnızca PurchaseIn ve CustomerReturn bakiyeyi
        /// artırır; sayım düzeltmesi dahil diğer tipler çıkış kabul edilir.
        /// </summary>
        private static bool IsIncoming(EnumStockMovementType movementType)
            => movementType is EnumStockMovementType.PurchaseIn
                or EnumStockMovementType.CustomerReturn;

        /// <summary>Hareket defteri kaydı öldüğündan güncelleme apısı bilinçli olarak yoktur.
        /// Hatalı hareket düzeltilmek istenirse ters yönlü yeni hareket yazılır.</summary>
    }
}
