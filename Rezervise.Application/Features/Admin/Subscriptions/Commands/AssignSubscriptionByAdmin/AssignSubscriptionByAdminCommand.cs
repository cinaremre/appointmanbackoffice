using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Commands.AssignSubscriptionByAdmin
{
    /// <summary>
    /// Backoffice ucu: isletmeye abonelik plani atar (dokuman Modul 13).
    /// Eski aktif abonelikleri Expired yapan domain mantigi mevcut
    /// AssignSubscriptionCommand icindedir; burada yalnizca iletilen
    /// degerler cevrilir ve islem denetimi yazilir.
    /// </summary>
    public class AssignSubscriptionByAdminCommand : IRequest<Result<Guid>>
    {
        /// <summary>Route'tan gelir; govdeden gelen deger ezilir.</summary>
        public Guid BusinessId { get; set; }

        public Guid PlanId { get; set; }

        /// <summary>Abonelik baslangici (UTC).</summary>
        public DateTime StartAt { get; set; }

        /// <summary>Abonelik sonu (UTC); null = belirsiz sure.</summary>
        public DateTime? EndAt { get; set; }
    }
}
