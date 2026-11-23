using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Queries.GetLowStockProducts
{
    /// <summary>
    /// Bakiyesi kritik stok seviyesinin altına düşmüş aktif ürünleri listeler.
    /// Bakiye stok hareketlerinin toplamından hesaplanır (Product üzerinde
    /// bakiye kolonu tutulmaz).
    /// </summary>
    public class GetLowStockProductsQuery : IRequest<Result<List<LowStockProductDto>>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>Kritik seviyeye düşmüş ürün satırı.</summary>
    public class LowStockProductDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        /// <summary>Mevcut bakiye (hareket toplamı).</summary>
        public decimal Balance { get; set; }

        /// <summary>Kritik stok seviyesi.</summary>
        public decimal MinimumStock { get; set; }

        /// <summary>Eksik miktar (MinimumStock - Balance).</summary>
        public decimal Shortage { get; set; }
    }
}
