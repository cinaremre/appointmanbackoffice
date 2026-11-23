using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Commands.AssignSubscriptionByAdmin
{
    /// <summary>
    /// Backoffice ucu: işletmeye abonelik planı atar (doküman Modül 13).
    /// Eski aktif abonelikleri Expired yapan domain mantığı mevcut
    /// AssignSubscriptionCommand içindedir; burada yalnızca iletilen
    /// değerler çevrilir ve işlem denetimi yazılır.
    /// </summary>
    public class AssignSubscriptionByAdminCommand : IRequest<Result<Guid>>
    {
        /// <summary>Route'tan gelir; gövdeden gelen değer ezilir.</summary>
        public Guid BusinessId { get; set; }

        public Guid PlanId { get; set; }

        /// <summary>Abonelik başlangıcı (UTC).</summary>
        public DateTime StartAt { get; set; }

        /// <summary>Abonelik sonu (UTC); null = belirsiz süre.</summary>
        public DateTime? EndAt { get; set; }
    }
}
