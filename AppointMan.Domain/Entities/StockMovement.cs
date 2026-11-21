using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Stok hareketi. Hareketler defter (ledger) mantigiyla calisir:
    /// bir kez yazilir, guncellenmez veya silinmez (Update metodu yoktur).
    /// Giris tipleri (PurchaseIn, CustomerReturn) pozitif, cikis tipleri
    /// negatif isaretle saklanir; boylece bakiye = hareketlerin toplami.
    /// Miktar cagri tarafindan her zaman pozitif verilir, isareti
    /// constructor hareket tipinden belirler.
    /// </summary>
    public class StockMovement : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        public Guid ProductId { get; private set; }

        public EnumStockMovementType MovementType { get; private set; }

        /// <summary>
        /// Isaretli miktar: giris hareketlerinde pozitif, cikis
        /// hareketlerinde negatif. Bakiye hesabi dogrudan toplanir.
        /// </summary>
        public decimal Quantity { get; private set; }

        /// <summary>Birim maliyet (girisler icin alis fiyati, cikislar icin 0 olabilir).</summary>
        public decimal UnitCost { get; private set; }

        /// <summary>Referans kayit tipi (orn. "Appointment", "Sale"). Cift isleme engeli icin kullanilir.</summary>
        public string? ReferenceType { get; private set; }

        /// <summary>Referans kayit ID'si. Ayni (ReferenceType, ReferenceId) ikinci kez islenemez.</summary>
        public Guid? ReferenceId { get; private set; }

        /// <summary>Hareketin gerceklestigi an (UTC).</summary>
        public DateTime OccurredAt { get; private set; }

        private StockMovement() { } // EF Core icin

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
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (productId == Guid.Empty)
                throw new ArgumentException("Urun ID bos olamaz.", nameof(productId));

            // Miktar her zaman pozitif girilir; isareti hareket tipi belirler.
            if (quantity <= 0)
                throw new DomainException("Stok hareket miktari sifirdan buyuk olmalidir.");

            if (quantity > 99999999.99m)
                throw new DomainException("Stok hareket miktari cok yuksek.");

            if (unitCost < 0)
                throw new DomainException("Birim maliyet negatif olamaz.");

            if (unitCost > 99999999.99m)
                throw new DomainException("Birim maliyet cok yuksek.");

            if (occurredAt == default)
                throw new DomainException("Hareket tarihi bos olamaz.");

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
        /// Giris tipi mi? Yalnizca PurchaseIn ve CustomerReturn bakiyeyi
        /// artirir; sayim duzeltmesi dahil diger tipler cikis kabul edilir.
        /// </summary>
        private static bool IsIncoming(EnumStockMovementType movementType)
            => movementType is EnumStockMovementType.PurchaseIn
                or EnumStockMovementType.CustomerReturn;

        /// <summary>Hareket defteri kaydi oldugundan guncelleme apisi bilincli olarak yoktur.
        /// Hatali hareket duzeltilmek istenirse ters yonlu yeni hareket yazilir.</summary>
    }
}
