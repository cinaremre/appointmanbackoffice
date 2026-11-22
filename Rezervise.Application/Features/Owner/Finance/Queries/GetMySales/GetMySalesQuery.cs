using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Owner.Finance.Queries.GetMySales
{
    /// <summary>
    /// Isletme sahibinin satis listesi. Sahiplik handler'da dogrulanir;
    /// tarih araligi filtresi desteklenir ve filtre kapsaminin toplam
    /// tutari yanitla birlikte doner (AdminSales deseni).
    /// </summary>
    public class GetMySalesQuery : IRequest<Result<OwnerSalesListDto>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Satis tarihi alt siniri (UTC, dahil).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Satis tarihi ust siniri (UTC, dahil).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Sayfali satis listesi + filtre geneli toplam tutar.</summary>
    public class OwnerSalesListDto
    {
        public IReadOnlyList<OwnerSaleDto> Items { get; init; } = Array.Empty<OwnerSaleDto>();

        public int Page { get; init; }

        public int PageSize { get; init; }

        public long TotalCount { get; init; }

        /// <summary>Filtreyle eslesen tum satislarin GrandTotal toplami.</summary>
        public decimal TotalGrandTotal { get; init; }
    }

    /// <summary>Owner satis satiri.</summary>
    public class OwnerSaleDto
    {
        public Guid Id { get; init; }

        public DateTime SoldAt { get; init; }

        public decimal GrandTotal { get; init; }

        public string Currency { get; init; } = string.Empty;

        /// <summary>Durum sayisal deger olarak doner (EnumSaleStatus).</summary>
        public EnumSaleStatus Status { get; init; }
    }
}
