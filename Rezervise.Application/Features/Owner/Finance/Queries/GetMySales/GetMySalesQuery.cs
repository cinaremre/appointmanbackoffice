using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Owner.Finance.Queries.GetMySales
{
    /// <summary>
    /// İşletme sahibinin satış listesi. Sahiplik handler'da doğrulanır;
    /// tarih aralığı filtresi desteklenir ve filtre kapsamının toplam
    /// tutarı yanıtla birlikte döner (AdminSales deseni).
    /// </summary>
    public class GetMySalesQuery : IRequest<Result<OwnerSalesListDto>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Satış tarihi alt sınırı (UTC, dahil).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Satış tarihi üst sınırı (UTC, dahil).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sınırlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Sayfalı satış listesi + filtre geneli toplam tutar.</summary>
    public class OwnerSalesListDto
    {
        public IReadOnlyList<OwnerSaleDto> Items { get; init; } = Array.Empty<OwnerSaleDto>();

        public int Page { get; init; }

        public int PageSize { get; init; }

        public long TotalCount { get; init; }

        /// <summary>Filtreyle eşleşen tüm satışların GrandTotal toplamı.</summary>
        public decimal TotalGrandTotal { get; init; }
    }

    /// <summary>Owner satış satırı.</summary>
    public class OwnerSaleDto
    {
        public Guid Id { get; init; }

        public DateTime SoldAt { get; init; }

        public decimal GrandTotal { get; init; }

        public string Currency { get; init; } = string.Empty;

        /// <summary>Durum sayısal değer olarak döner (EnumSaleStatus).</summary>
        public EnumSaleStatus Status { get; init; }
    }
}
