using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Notifications.Queries.GetNotificationById
{
    public class GetNotificationByIdQuery : IRequest<Result<NotificationDto>>
    {
        public Guid NotificationId { get; set; }
    }
}