using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Notifications.Queries.GetAdminNotifications
{
    /// <summary>
    /// Bildirim listesini filtreler, gonderim zamanina gore tersten siralar ve
    /// sayfalar. Yetki kontrolu API katmanindaki permission policy ile yapilir.
    /// </summary>
    public class GetAdminNotificationsQueryHandler
        : IRequestHandler<GetAdminNotificationsQuery, Result<AdminNotificationListDto>>
    {
        private readonly IRepository<Notification> _notificationRepository;

        public GetAdminNotificationsQueryHandler(IRepository<Notification> notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result<AdminNotificationListDto>> Handle(
            GetAdminNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Filtre kosullari lambda icinde kapanis degiskenlariyla tasinir;
                // null filtreler dogal olarak devre disi kalir.
                var userId = request.UserId;
                var type = request.Type;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                var notifications = await _notificationRepository.FindAsync(
                    n => !n.IsDeleted
                        && (userId == null || n.UserId == userId)
                        && (type == null || n.Type == type)
                        && (fromDate == null || n.SentAt >= fromDate)
                        && (toDate == null || n.SentAt <= toDate),
                    cancellationToken);

                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var ordered = notifications
                    .OrderByDescending(n => n.SentAt)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(ToDto)
                    .ToList();

                return Result<AdminNotificationListDto>.Success(new AdminNotificationListDto
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = ordered.Count
                });
            }
            catch (Exception ex)
            {
                return Result<AdminNotificationListDto>.Failure(
                    Error.Internal("Bildirim listesi getirilirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>Entity uzerinden yonetici gorunumu DTO'su uretir.</summary>
        private static AdminNotificationDto ToDto(Notification notification) => new()
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            IsRead = notification.IsRead,
            SentAt = notification.SentAt
        };
    }
}
