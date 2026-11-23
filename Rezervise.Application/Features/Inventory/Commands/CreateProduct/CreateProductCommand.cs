using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.CreateProduct
{
    /// <summary>
    /// İşletmeye yeni stok ürünü tanımlar. SKU işletme bazında benzersizdir.
    /// OwnerId gövdeden değil JWT'den çözülür (Api katmanı set eder).
    /// </summary>
    public class CreateProductCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        /// <summary>İşletme içinde benzersiz stok kodu.</summary>
        public string Sku { get; set; } = string.Empty;

        public string? Barcode { get; set; }

        public string Name { get; set; } = string.Empty;

        public Guid? CategoryId { get; set; }

        /// <summary>Ölçü birimi (varsayılan "adet").</summary>
        public string Unit { get; set; } = "adet";

        public decimal PurchasePrice { get; set; }

        public decimal SalePrice { get; set; }

        public decimal MinimumStock { get; set; }
    }
}
