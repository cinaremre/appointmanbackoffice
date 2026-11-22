using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.CreateProduct
{
    /// <summary>
    /// Isletmeye yeni stok urunu tanimlar. SKU isletme bazinda benzersizdir.
    /// OwnerId govdeden degil JWT'den cozulur (Api katmani set eder).
    /// </summary>
    public class CreateProductCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        /// <summary>Isletme icinde benzersiz stok kodu.</summary>
        public string Sku { get; set; } = string.Empty;

        public string? Barcode { get; set; }

        public string Name { get; set; } = string.Empty;

        public Guid? CategoryId { get; set; }

        /// <summary>Olcu birimi (varsayilan "adet").</summary>
        public string Unit { get; set; } = "adet";

        public decimal PurchasePrice { get; set; }

        public decimal SalePrice { get; set; }

        public decimal MinimumStock { get; set; }
    }
}
