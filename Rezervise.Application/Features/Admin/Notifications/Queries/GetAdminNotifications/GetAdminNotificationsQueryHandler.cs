using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotifications
{
    /// <summary>
    /// Bildirim listesini filtreler, gönderim zamanına göre tersten sıralar ve
    /// sayfalar. Yetki kontrolü API katmanındaki permission policy ile yapılır.
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
                // Filtre koşulları lambda içinde kapanış değişkenlarıyla taşınır;
                // null filtreler doğal olarak devre dışı kalır.
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
                    Error.Internal("Bildirim listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>Entity üzerinden yönetici görünümü DTO'su üretir.</summary>
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
