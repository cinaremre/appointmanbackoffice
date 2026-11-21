using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Subscriptions.Commands.AssignSubscription
{
    /// <summary>
    /// Isletmeye abonelik plani atar (dokuman Modul 14).
    /// Isletmenin varsa aktif aboneligi Expired yapilir.
    /// Yalniz platform yoneticileri cagirabilir.
    /// </summary>
    public class AssignSubscriptionCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid PlanId { get; set; }

        /// <summary>Abonelik baslangici (UTC).</summary>
        public DateTime StartAt { get; set; }

        /// <summary>Abonelik sonu (UTC); null = belirsiz sure.</summary>
        public DateTime? EndAt { get; set; }
    }
}
