using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Sales.Commands.RefundPayment
{
    /// <summary>
    /// Bir ödemeye bağlı ters (iade) kaydı oluşturur. Negatif tutar yerine
    /// IsRefund=true olan ayrı bir Payment kaydı açılır; nakit iadelerde
    /// açık kasa oturumuna bir de CashOut hareketi yazılır.
    /// </summary>
    public class RefundPaymentCommand : IRequest<Result<Guid>>
    {
        public Guid PaymentId { get; set; }

        /// <summary>İade tutarı; orijinal ödemeyi ve önceki iadeleri geçemez.</summary>
        public decimal Amount { get; set; }

        /// <summary>İade gerekçesi; kasa hareketine taşınır.</summary>
        public string? Reason { get; set; }
    }
}
