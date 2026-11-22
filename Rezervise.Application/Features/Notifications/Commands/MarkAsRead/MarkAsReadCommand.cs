using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Notifications.Commands.MarkAsRead
{
    public class MarkAsReadCommand : IRequest<Result<Guid>>
    {
        public Guid NotificationId { get; set; }
    }
}