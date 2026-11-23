using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotifications
{
    /// <summary>
    /// Backoffice bildirim satırı. Mevcut in-app Notification entity'sinin
    /// yönetici görünümü için sadeleştirilmiş kopyası.
    /// </summary>
    public class AdminNotificationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public EnumNotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
    }

    /// <summary>Sayfalı bildirim listesi sonucu.</summary>
    public class AdminNotificationListDto
    {
        public IReadOnlyList<AdminNotificationDto> Items { get; set; } = Array.Empty<AdminNotificationDto>();

        /// <summary>Kırpanma sonrası uygulanan sayfa numarası.</summary>
        public int Page { get; set; }

        /// <summary>Kırpanma sonrası uygulanan sayfa boyutu.</summary>
        public int PageSize { get; set; }

        /// <summary>Filtreye uyan toplam bildirim sayısı.</summary>
        public long TotalCount { get; set; }
    }
}
