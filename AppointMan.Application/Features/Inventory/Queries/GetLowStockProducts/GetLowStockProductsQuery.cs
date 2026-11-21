using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Inventory.Queries.GetLowStockProducts
{
    /// <summary>
    /// Bakiyesi kritik stok seviyesinin altina dusmus aktif urunleri listeler.
    /// Bakiye stok hareketlerinin toplamindan hesaplanir (Product uzerinde
    /// bakiye kolonu tutulmaz).
    /// </summary>
    public class GetLowStockProductsQuery : IRequest<Result<List<LowStockProductDto>>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>Kritik seviyeye dusmus urun satiri.</summary>
    public class LowStockProductDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        /// <summary>Mevcut bakiye (hareket toplami).</summary>
        public decimal Balance { get; set; }

        /// <summary>Kritik stok seviyesi.</summary>
        public decimal MinimumStock { get; set; }

        /// <summary>Eksik miktar (MinimumStock - Balance).</summary>
        public decimal Shortage { get; set; }
    }
}
