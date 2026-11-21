using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Owner.Stock.Queries.GetMyStockMovements
{
    /// <summary>
    /// Isletme sahibinin stok hareket listesi. Hareketler defter
    /// (ledger) kayitlaridir; oldugu gibi listelenir.
    /// </summary>
    public class GetMyStockMovementsQuery : IRequest<Result<AdminPagedResult<OwnerStockMovementDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Yalnizca bu urunun hareketleri.</summary>
        public Guid? ProductId { get; set; }

        /// <summary>Hareket tarihi alt siniri (UTC, dahil).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Hareket tarihi ust siniri (UTC, dahil).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner stok hareket satiri.</summary>
    public class OwnerStockMovementDto
    {
        public Guid Id { get; init; }

        public Guid ProductId { get; init; }

        /// <summary>Hareket tipi sayisal deger olarak doner.</summary>
        public EnumStockMovementType MovementType { get; init; }

        /// <summary>Isaretli miktar: giris pozitif, cikis negatif.</summary>
        public decimal Quantity { get; init; }

        public decimal UnitCost { get; init; }

        public string? ReferenceType { get; init; }

        public Guid? ReferenceId { get; init; }

        public DateTime OccurredAt { get; init; }
    }
}
