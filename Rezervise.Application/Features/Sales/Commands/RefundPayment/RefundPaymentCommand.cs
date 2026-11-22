using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Sales.Commands.RefundPayment
{
    /// <summary>
    /// Bir odemeye bagli ters (iade) kaydi olusturur. Negatif tutar yerine
    /// IsRefund=true olan ayri bir Payment kaydi acilir; nakit iadelerde
    /// acik kasa oturumuna bir de CashOut hareketi yazilir.
    /// </summary>
    public class RefundPaymentCommand : IRequest<Result<Guid>>
    {
        public Guid PaymentId { get; set; }

        /// <summary>Iade tutari; orijinal odemeyi ve onceki iadeleri gecemez.</summary>
        public decimal Amount { get; set; }

        /// <summary>Iade gerekcesi; kasa hareketine tasinir.</summary>
        public string? Reason { get; set; }
    }
}
