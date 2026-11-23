using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Reengagement.Commands.MarkReengagementNotified
{
    /// <summary>
    /// Aday listesinden seçilen müşteri için bildirim gönderildi işareti ekler
    /// (doküman Modül 13). Yeni ReengagementAttempt kaydı Notified durumunda oluşur.
    /// </summary>
    public class MarkReengagementNotifiedCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Müşterinin User ID'si.</summary>
        public Guid CustomerId { get; set; }

        public Guid ServiceId { get; set; }
        public Guid RuleId { get; set; }

        /// <summary>Gönderim varsa bildirim kaydının ID'si.</summary>
        public Guid? NotificationId { get; set; }
    }
}
