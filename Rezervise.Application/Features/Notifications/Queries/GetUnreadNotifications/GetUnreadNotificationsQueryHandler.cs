using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Notifications.Queries.GetUnreadNotifications;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Notifications.Queries.GetUnreadNotifications
{
    public class GetUnreadNotificationsQueryHandler : IRequestHandler<GetUnreadNotificationsQuery, Result<List<NotificationDto>>>
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly ICurrentUserService _currentUser;

        public GetUnreadNotificationsQueryHandler(
            IRepository<Notification> notificationRepository,
            ICurrentUserService currentUser)
        {
            _notificationRepository = notificationRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<List<NotificationDto>>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Kimlik her zaman imzali token'dan gelir; istekteki hicbir alan kullaniciyi temsil etmez.
                var currentUserId = _currentUser.UserId;
                if (currentUserId is null)
                    return Result<List<NotificationDto>>.Failure(Error.Unauthorized("Auth.InvalidUser"));

                // 1. Okunmamis bildirimleri getir
                var notifications = await _notificationRepository.FindAsync(
                    n => n.UserId == currentUserId.Value && !n.IsRead && !n.IsDeleted,
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