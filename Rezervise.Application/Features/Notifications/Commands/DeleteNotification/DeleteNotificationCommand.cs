using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Notifications.Commands.DeleteNotification
{
    public class DeleteNotificationCommand : IRequest<Result<Guid>>
    {
        public Guid NotificationId { get; set; }
    }
}