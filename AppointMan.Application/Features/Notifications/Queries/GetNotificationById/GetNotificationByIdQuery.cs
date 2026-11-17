using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Notifications.Queries.GetNotificationById
{
    public class GetNotificationByIdQuery : IRequest<Result<NotificationDto>>
    {
        public Guid NotificationId { get; set; }
    }
}