using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Notifications.Commands.MarkAllAsRead
{
    public class MarkAllAsReadCommand : IRequest<Result<int>>
    {
        public Guid UserId { get; set; }
    }
}