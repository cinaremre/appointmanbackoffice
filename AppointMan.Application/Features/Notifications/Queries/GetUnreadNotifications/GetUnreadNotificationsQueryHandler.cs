using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Notifications.Queries.GetUnreadNotifications;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Notifications.Queries.GetUnreadNotifications
{
    public class GetUnreadNotificationsQueryHandler : IRequestHandler<GetUnreadNotificationsQuery, Result<List<NotificationDto>>>
    {
        private readonly IRepository<Notification> _notificationRepository;

        public GetUnreadNotificationsQueryHandler(IRepository<Notification> notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result<List<NotificationDto>>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Okunmamis bildirimleri getir
                var notifications = await _notificationRepository.FindAsync(
                    n => n.UserId == request.UserId && !n.IsRead && !n.IsDeleted,
                    cancellationToken);

                // 2. Siralama
                var unreadNotifications = notifications
                    .OrderByDescending(n => n.SentAt)
                    .ToList();

                // 3. DTO'ya donustur
                var dtos = unreadNotifications.Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    SentAt = n.SentAt,
                    ActionUrl = n.ActionUrl,
                    Metadata = n.Metadata
                }).ToList();

                return Result<List<NotificationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<NotificationDto>>.Failure(
                    Error.Internal("Okunmamis bildirimler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}