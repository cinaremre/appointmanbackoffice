using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Stock.Queries.GetAdminProducts
{
    /// <summary>
    /// Ürün listesi + stok bakiyesi (doküman Modül 17). Bakiye hareket
    /// defterinden (StockMovement) toplanır; Product üzerinde bakiye
    /// tutulmaz. İlk sürümde sayfadaki ürünlerin hareketleri belleke
    /// çekilip toplanır (bellek içi hesap, doküman 17).
    /// </summary>
    public class GetAdminProductsQuery : IRequest<Result<AdminPagedResult<AdminProductDto>>>
    {
        public Guid? BusinessId { get; set; }

        /// <summary>Ürün adı veya SKU içinde aranır; boş ise filtre uygulanmaz.</summary>
        public string? Search { get; set; }

        public bool? IsActive { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Tek ürünün stok bakiyesiyle birlikte admin görünümü.</summary>
    public class AdminProductDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinimumStock { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Hareket defterinin işaretli toplamı; girişler pozitif, çıkışlar negatif.</summary>
        public decimal StockBalance { get; set; }
    }
}
