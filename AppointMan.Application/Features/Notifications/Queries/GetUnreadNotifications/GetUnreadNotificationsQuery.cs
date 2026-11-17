using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Notifications.Queries.GetUnreadNotifications
{
    public class GetUnreadNotificationsQuery : IRequest<Result<List<NotificationDto>>>
    {
    }
}