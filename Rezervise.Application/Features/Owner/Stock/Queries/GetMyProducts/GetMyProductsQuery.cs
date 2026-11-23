using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Stock.Queries.GetMyProducts
{
    /// <summary>
    /// İşletme sahibinin ürün listesi. Sahiplik handler'da doğrulanır;
    /// stok bakiyesi hareket defterinin işaretli toplamıdır.
    /// </summary>
    public class GetMyProductsQuery : IRequest<Result<AdminPagedResult<OwnerProductDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Ürün ad/SKU içerik araması.</summary>
        public string? Search { get; set; }

        /// <summary>Aktif/pasif ürün filtresi.</summary>
        public bool? IsActive { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sınırlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner ürün satırı; defterden hesaplanan bakiyeyle.</summary>
    public class OwnerProductDto
    {
        public Guid Id { get; init; }

        public string Sku { get; init; } = string.Empty;

        public string? Barcode { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Unit { get; init; } = string.Empty;

        public decimal PurchasePrice { get; init; }

        public decimal SalePrice { get; init; }

        public decimal MinimumStock { get; init; }

        public bool IsActive { get; init; }

        /// <summary>Hareket defterinin işaretli toplamı (giriş - çıkış).</summary>
        public decimal StockBalance { get; init; }
    }
}
