using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.RecordStockMovement
{
    /// <summary>
    /// Elle stok hareketi kaydeder (satın alma girişi, satış çıkışı,
    /// zayı, sayım düzeltmesi, ...). Miktar her zaman pozitif verilir;
    /// işareti hareket tipinden hareket kendisi belirler.
    /// Aynı (ReferenceType, ReferenceId) referansıyla ikinci hareket
    /// kaydedilmez (çift işleme engeli).
    /// OwnerId gövdeden değil JWT'den çözülür (Api katmanı set eder).
    /// </summary>
    public class RecordStockMovementCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        public Guid ProductId { get; set; }

        public EnumStockMovementType MovementType { get; set; }

        /// <summary>Pozitif miktar; işaret hareket tipinden belirlenir.</summary>
        public decimal Quantity { get; set; }

        /// <summary>Birim maliyet (opsiyonel; girişlerde alış fiyatı).</summary>
        public decimal? UnitCost { get; set; }

        /// <summary>Referans kayıt tipi (opsiyonel; çift işleme engeli için).</summary>
        public string? ReferenceType { get; set; }

        /// <summary>Referans kayıt ID'si (opsiyonel).</summary>
        public Guid? ReferenceId { get; set; }

        /// <summary>Hareket anı (UTC); verilmezse su an kullanılır.</summary>
        public DateTime? OccurredAt { get; set; }
    }
}
