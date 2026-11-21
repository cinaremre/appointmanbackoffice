using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Satis satiri. Satir toplami: LineTotal = Quantity * UnitPrice
    /// - DiscountAmount + TaxAmount. Olusan tutar Sale aggregate'i
    /// uzerindeki genel toplamlara yansitilir.
    /// </summary>
    public class SaleLine : BaseEntity
    {
        public Guid SaleId { get; private set; }
        public EnumSaleItemType ItemType { get; private set; }
        public Guid? ServiceId { get; private set; }
        public Guid? ProductId { get; private set; }
        public decimal Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal DiscountAmount { get; private set; }
        public decimal TaxAmount { get; private set; }
        public decimal LineTotal { get; private set; }
        public Guid EmployeeId { get; private set; }

        public Sale Sale { get; private set; }

        private SaleLine() { }

        public SaleLine(Guid saleId, EnumSaleItemType itemType,
            Guid? serviceId, Guid? productId,
            decimal quantity, decimal unitPrice, decimal discountAmount, decimal taxAmount,
            Guid employeeId)
        {
            if (saleId == Guid.Empty)
                throw new ArgumentException("Satis ID bos olamaz.", nameof(saleId));

            if (employeeId == Guid.Empty)
                throw new ArgumentException("Calisan ID bos olamaz.", nameof(employeeId));

            if (quantity <= 0)
                throw new DomainException("Satis satiri miktari sifirdan buyuk olmalidir.");

            if (unitPrice < 0)
                throw new DomainException("Satis satiri birim fiyati negatif olamaz.");

            var gross = Math.Round(quantity * unitPrice, 2);

            if (discountAmount < 0 || discountAmount > gross)
                throw new DomainException("Satis satiri indirimi satir brut tutarindan buyuk olamaz.");

            if (taxAmount < 0)
                throw new DomainException("Satis satiri vergisi negatif olamaz.");

            if (itemType == EnumSaleItemType.Service && serviceId is null)
                throw new DomainException("Hizmet satirinda ServiceId zorunludur.");

            if (itemType == EnumSaleItemType.Product && productId is null)
                throw new DomainException("Urun satirinda ProductId zorunludur.");

            SaleId = saleId;
            ItemType = itemType;
            ServiceId = serviceId;
            ProductId = productId;
            Quantity = quantity;
            UnitPrice = unitPrice;
            DiscountAmount = Math.Round(discountAmount, 2);
            TaxAmount = Math.Round(taxAmount, 2);
            LineTotal = Math.Round(gross - DiscountAmount + TaxAmount, 2);
            EmployeeId = employeeId;
        }
    }
}
