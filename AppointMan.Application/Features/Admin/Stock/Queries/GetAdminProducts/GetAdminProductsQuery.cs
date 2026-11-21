using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Stock.Queries.GetAdminProducts
{
    /// <summary>
    /// Urun listesi + stok bakiyesi (dokuman Modul 17). Bakiye hareket
    /// defterinden (StockMovement) toplanir; Product uzerinde bakiye
    /// tutulmaz. Ilk surumde sayfadaki urunlerin hareketleri belleke
    /// cekilip toplanir (bellek ici hesap, dokuman 17).
    /// </summary>
    public class GetAdminProductsQuery : IRequest<Result<AdminPagedResult<AdminProductDto>>>
    {
        public Guid? BusinessId { get; set; }

        /// <summary>Urun adi veya SKU icinde aranir; bos ise filtre uygulanmaz.</summary>
        public string? Search { get; set; }

        public bool? IsActive { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Tek urunun stok bakiyesiyle birlikte admin gorunumu.</summary>
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

        /// <summary>Hareket defterinin isaretli toplami; girisler pozitif, cikislar negatif.</summary>
        public decimal StockBalance { get; set; }
    }
}
