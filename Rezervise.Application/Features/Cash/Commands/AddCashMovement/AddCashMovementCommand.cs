using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.AddCashMovement
{
    /// <summary>
    /// Acik kasa oturumuna manuel nakit girisi/cikisi ekler.
    /// </summary>
    public class AddCashMovementCommand : IRequest<Result<Guid>>
    {
        public Guid CashSessionId { get; set; }

        public EnumCashMovementType MovementType { get; set; }

        /// <summary>Hareket tutari; her zaman pozitiftir, yonu MovementType ile belirlenir.</summary>
        public decimal Amount { get; set; }

        /// <summary>Hareket gerekcesi.</summary>
        public string? Reason { get; set; }
    }
}
