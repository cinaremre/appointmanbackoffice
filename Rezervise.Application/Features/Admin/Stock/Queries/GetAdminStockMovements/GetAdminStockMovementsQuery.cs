using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Stock.Queries.GetAdminStockMovements
{
    /// <summary>
    /// Stok hareket listesi (dokuman Modul 17). Hareket defteri kayitlari
    /// salt okunur listelenir; duzeltme ters yonlu yeni hareketle yapilir.
    /// </summary>
    public class GetAdminStockMovementsQuery : IRequest<Result<AdminPagedResult<AdminStockMovementDto>>>
    {
        public Guid? BusinessId { get; set; }
        public Guid? ProductId { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Tek stok hareketinin admin gorunumu.</summary>
    public class AdminStockMovementDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid ProductId { get; set; }
        public EnumStockMovementType MovementType { get; set; }

        /// <summary>Isaretli miktar: giriste pozitif, cikista negatif.</summary>
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }

        public string? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
