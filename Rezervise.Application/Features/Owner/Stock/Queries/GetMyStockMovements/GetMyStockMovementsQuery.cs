using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Owner.Stock.Queries.GetMyStockMovements
{
    /// <summary>
    /// İşletme sahibinin stok hareket listesi. Hareketler defter
    /// (ledger) kayıtlarıdır; öldüğü gibi listelenir.
    /// </summary>
    public class GetMyStockMovementsQuery : IRequest<Result<AdminPagedResult<OwnerStockMovementDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Yalnızca bu ürünün hareketleri.</summary>
        public Guid? ProductId { get; set; }

        /// <summary>Hareket tarihi alt sınırı (UTC, dahil).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Hareket tarihi üst sınırı (UTC, dahil).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sınırlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner stok hareket satırı.</summary>
    public class OwnerStockMovementDto
    {
        public Guid Id { get; init; }

        public Guid ProductId { get; init; }

        /// <summary>Hareket tipi sayısal değer olarak döner.</summary>
        public EnumStockMovementType MovementType { get; init; }

        /// <summary>İşaretli miktar: giriş pozitif, çıkış negatif.</summary>
        public decimal Quantity { get; init; }

        public decimal UnitCost { get; init; }

        public string? ReferenceType { get; init; }

        public Guid? ReferenceId { get; init; }

        public DateTime OccurredAt { get; init; }
    }
}
