using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Notifications.Commands.MarkAsRead
{
    public class MarkAsReadCommand : IRequest<Result<Guid>>
    {
        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }
    }
}