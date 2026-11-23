using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.AddCashMovement
{
    /// <summary>
    /// Açık kasa oturumuna manuel nakit girişi/çıkışı ekler.
    /// </summary>
    public class AddCashMovementCommand : IRequest<Result<Guid>>
    {
        public Guid CashSessionId { get; set; }

        public EnumCashMovementType MovementType { get; set; }

        /// <summary>Hareket tutarı; her zaman pozitiftir, yönü MovementType ile belirlenir.</summary>
        public decimal Amount { get; set; }

        /// <summary>Hareket gerekçesi.</summary>
        public string? Reason { get; set; }
    }
}
