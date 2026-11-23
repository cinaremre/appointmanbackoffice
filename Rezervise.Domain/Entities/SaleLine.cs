using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Satış satırı. Satır toplamı: LineTotal = Quantity * UnitPrice
    /// - DiscountAmount + TaxAmount. Oluşan tutar Sale aggregate'i
    /// üzerindeki genel toplamlara yansıtılır.
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
                throw new ArgumentException("Satış ID boş olamaz.", nameof(saleId));

            if (employeeId == Guid.Empty)
                throw new ArgumentException("Çalışan ID boş olamaz.", nameof(employeeId));

            if (quantity <= 0)
                throw new DomainException("Satış satırı miktarı sıfırdan büyük olmalıdır.");

            if (unitPrice < 0)
                throw new DomainException("Satış satırı birim fiyatı negatif olamaz.");

            var gross = Math.Round(quantity * unitPrice, 2);

            if (discountAmount < 0 || discountAmount > gross)
                throw new DomainException("Satış satırı indirimi satır brüt tutarından büyük olamaz.");

            if (taxAmount < 0)
                throw new DomainException("Satış satırı vergisi negatif olamaz.");

            if (itemType == EnumSaleItemType.Service && serviceId is null)
                throw new DomainException("Hizmet satırında ServiceId zorunludur.");

            if (itemType == EnumSaleItemType.Product && productId is null)
                throw new DomainException("Ürün satırında ProductId zorunludur.");

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
