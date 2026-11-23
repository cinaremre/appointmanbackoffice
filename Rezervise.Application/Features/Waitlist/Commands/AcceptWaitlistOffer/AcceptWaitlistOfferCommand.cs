using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Waitlist.Commands.AcceptWaitlistOffer
{
    /// <summary>
    /// Müşteri bekleme listesi teklifini kabul eder (doküman Modül 10).
    /// Müşteri kimliği teklifin kaydından çözülür; gövdeye güvenilmez.
    /// Kabul anında slot yeniden müsaitlik kontrolünden geçer; aynı slotun
    /// diğer Pending teklifleri Superseded edilir.
    /// </summary>
    public class AcceptWaitlistOfferCommand : IRequest<Result<Guid>>
    {
        public Guid OfferId { get; set; }
    }
}
