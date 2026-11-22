using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.RecordStockMovement
{
    /// <summary>
    /// Elle stok hareketi kaydeder (satin alma girisi, satis cikisi,
    /// zayi, sayim duzeltmesi, ...). Miktar her zaman pozitif verilir;
    /// isareti hareket tipinden hareket kendisi belirler.
    /// Ayni (ReferenceType, ReferenceId) referansiyla ikinci hareket
    /// kaydedilmez (cift isleme engeli).
    /// OwnerId govdeden degil JWT'den cozulur (Api katmani set eder).
    /// </summary>
    public class RecordStockMovementCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        public Guid ProductId { get; set; }

        public EnumStockMovementType MovementType { get; set; }

        /// <summary>Pozitif miktar; isaret hareket tipinden belirlenir.</summary>
        public decimal Quantity { get; set; }

        /// <summary>Birim maliyet (opsiyonel; girislerde alis fiyati).</summary>
        public decimal? UnitCost { get; set; }

        /// <summary>Referans kayit tipi (opsiyonel; cift isleme engeli icin).</summary>
        public string? ReferenceType { get; set; }

        /// <summary>Referans kayit ID'si (opsiyonel).</summary>
        public Guid? ReferenceId { get; set; }

        /// <summary>Hareket ani (UTC); verilmezse su an kullanilir.</summary>
        public DateTime? OccurredAt { get; set; }
    }
}
