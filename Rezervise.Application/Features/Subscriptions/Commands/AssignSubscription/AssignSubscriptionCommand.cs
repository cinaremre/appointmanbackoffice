using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Subscriptions.Commands.AssignSubscription
{
    /// <summary>
    /// İşletmeye abonelik planı atar (doküman Modül 14).
    /// İşletmenin varsa aktif aboneliği Expired yapılır.
    /// Yalnız platform yöneticileri çağırabilir.
    /// </summary>
    public class AssignSubscriptionCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid PlanId { get; set; }

        /// <summary>Abonelik başlangıcı (UTC).</summary>
        public DateTime StartAt { get; set; }

        /// <summary>Abonelik sonu (UTC); null = belirsiz süre.</summary>
        public DateTime? EndAt { get; set; }
    }
}
