using AppointMan.Domain.Enums;

namespace AppointMan.Application.Features.Admin.Notifications.Queries.GetAdminNotifications
{
    /// <summary>
    /// Backoffice bildirim satiri. Mevcut in-app Notification entity'sinin
    /// yonetici gorunumu icin sadelestirilmis kopyasi.
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

    /// <summary>Sayfali bildirim listesi sonucu.</summary>
    public class AdminNotificationListDto
    {
        public IReadOnlyList<AdminNotificationDto> Items { get; set; } = Array.Empty<AdminNotificationDto>();

        /// <summary>Kirpanma sonrasi uygulanan sayfa numarasi.</summary>
        public int Page { get; set; }

        /// <summary>Kirpanma sonrasi uygulanan sayfa boyutu.</summary>
        public int PageSize { get; set; }

        /// <summary>Filtreye uyan toplam bildirim sayisi.</summary>
        public long TotalCount { get; set; }
    }
}
