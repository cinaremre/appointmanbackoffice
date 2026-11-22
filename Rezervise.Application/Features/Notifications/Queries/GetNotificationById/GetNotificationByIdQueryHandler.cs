using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Notifications.Queries.GetNotificationById
{
    public class GetNotificationByIdQueryHandler : IRequestHandler<GetNotificationByIdQuery, Result<NotificationDto>>
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly ICurrentUserService _currentUser;

        public GetNotificationByIdQueryHandler(
            IRepository<Notification> notificationRepository,
            ICurrentUserService currentUser)
        {
            _notificationRepository = notificationRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<NotificationDto>> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Bildirimi ID'ye gore getir (silinmis kayitlar haric)
                var notifications = await _notificationRepository.FindAsync(
                    n => n.Id == request.NotificationId && !n.IsDeleted,
                    cancellationToken);

                var notification = notifications.FirstOrDefault();

                // 2. Bildirim bulunamadiysa hata dondur
                if (notification is null)
                    return Result<NotificationDto>.Failure(Error.NotFound("Bildirim", request.NotificationId.ToString()));

                // 3. Yetki kontrolu.
                //    Aktor kimligi her zaman imzali token'dan gelir; istekteki hicbir alan
                //    kullaniciyi temsil etmez. Aksi halde istemci baskasinin bildirimini okur.
                var currentUserId = _currentUser.UserId;
                if (currentUserId is null)
                    return Result<NotificationDto>.Failure(Error.Unauthorized("Auth.InvalidUser"));

                if (notification.UserId != currentUserId.Value)
                    return Result<NotificationDto>.Failure(Error.Unauthorized("Bu bildirimi goruntuleme yetkiniz yok."));

                // 4. DTO'ya donustur
                var dto = new NotificationDto
                {
                    Id = notification.Id,
                    UserId = notification.UserId,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    IsRead = notification.IsRead,
                    ReadAt = notification.ReadAt,
                    SentAt = notification.SentAt,
                    ActionUrl = notification.ActionUrl,
                    Metadata = notification.Metadata
                };

                return Result<NotificationDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<NotificationDto>.Failure(Error.Internal("Bildirim bilgileri getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}