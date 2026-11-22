using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Reengagement.Commands.MarkReengagementNotified
{
    /// <summary>
    /// Aday listesinden secilen musteri icin bildirim gonderildi isareti ekler
    /// (dokuman Modul 13). Yeni ReengagementAttempt kaydi Notified durumunda olusur.
    /// </summary>
    public class MarkReengagementNotifiedCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Musterinin User ID'si.</summary>
        public Guid CustomerId { get; set; }

        public Guid ServiceId { get; set; }
        public Guid RuleId { get; set; }

        /// <summary>Gonderim varsa bildirim kaydinin ID'si.</summary>
        public Guid? NotificationId { get; set; }
    }
}
