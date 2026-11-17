using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Notifications.Commands.DeleteNotification
{
    public class DeleteNotificationCommand : IRequest<Result<Guid>>
    {
        public Guid NotificationId { get; set; }
    }
}