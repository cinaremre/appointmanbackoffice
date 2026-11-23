using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Notifications.Queries.GetNotificationsByUser;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Notifications.Queries.GetNotificationsByUser
{
    public class GetNotificationsByUserQueryHandler : IRequestHandler<GetNotificationsByUserQuery, Result<List<NotificationDto>>>
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly ICurrentUserService _currentUser;

        public GetNotificationsByUserQueryHandler(
            IRepository<Notification> notificationRepository,
            ICurrentUserService currentUser)
        {
            _notificationRepository = notificationRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<List<NotificationDto>>> Handle(GetNotificationsByUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Kimlik her zaman imzalı token'dan gelir; istekteki hiçbir alan kullanıcıyı temsil etmez.
                var currentUserId = _currentUser.UserId;
                if (currentUserId is null)
                    return Result<List<NotificationDto>>.Failure(Error.Unauthorized("Auth.InvalidUser"));

                // 1. Kullanıcının bildirimlerini getir
                var notifications = await _notificationRepository.FindAsync(
                    n => n.UserId == currentUserId.Value && !n.IsDeleted,
                    cancellationToken);

                // 2. Sayfalama
                var pagedNotifications = notifications
                    .OrderByDescending(n => n.SentAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 3. DTO'ya dönüştür
                var dtos = pagedNotifications.Select(n => new NotificationDto
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
                    Error.Internal("Bildirimler getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}