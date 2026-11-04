using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Notifications.Queries.GetNotificationById
{
    public class GetNotificationByIdQueryHandler : IRequestHandler<GetNotificationByIdQuery, Result<NotificationDto>>
    {
        private readonly IRepository<Notification> _notificationRepository;

        public GetNotificationByIdQueryHandler(IRepository<Notification> notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result<NotificationDto>> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Bildirimi ID'ye gore getir
                var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

                // 2. Bildirim bulunamadiysa hata dondur
                if (notification is null)
                    return Result<NotificationDto>.Failure(Error.NotFound("Bildirim", request.NotificationId.ToString()));

                // 3. Yetki kontrolu
                if (notification.UserId != request.UserId)
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