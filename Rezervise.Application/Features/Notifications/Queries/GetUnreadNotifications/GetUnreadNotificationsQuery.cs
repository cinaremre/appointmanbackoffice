using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Notifications.Queries.GetUnreadNotifications
{
    public class GetUnreadNotificationsQuery : IRequest<Result<List<NotificationDto>>>
    {
    }
}