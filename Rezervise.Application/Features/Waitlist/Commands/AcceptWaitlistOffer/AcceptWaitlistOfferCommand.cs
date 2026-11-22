using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Waitlist.Commands.AcceptWaitlistOffer
{
    /// <summary>
    /// Musteri bekleme listesi teklifini kabul eder (dokuman Modul 10).
    /// Musteri kimligi teklifin kaydindan cozulur; govdeye guvenilmez.
    /// Kabul aninda slot yeniden musaitlik kontrolunden gecer; ayni slotun
    /// diger Pending teklifleri Superseded edilir.
    /// </summary>
    public class AcceptWaitlistOfferCommand : IRequest<Result<Guid>>
    {
        public Guid OfferId { get; set; }
    }
}
