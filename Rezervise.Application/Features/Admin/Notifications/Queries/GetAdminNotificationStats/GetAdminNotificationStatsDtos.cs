using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotificationStats
{
    /// <summary>
    /// Son 30 günlük bildirim istatistikleri. Sağlayıcı bazlı gönderim maliyeti
    /// bu DTO'da henüz yer almaz; bkz. <see cref="CostNote"/>.
    /// </summary>
    public class AdminNotificationStatsDto
    {
        /// <summary>İstatistik penceresinin başlangıç anı (UTC).</summary>
        public DateTime FromDate { get; set; }

        /// <summary>İstatistik penceresinin bitiş anı (UTC).</summary>
        public DateTime ToDate { get; set; }

        /// <summary>Penceredeki toplam bildirim sayısı.</summary>
        public int Total { get; set; }

        /// <summary>Pencerede okunmuş bildirim sayısı.</summary>
        public int ReadCount { get; set; }

        /// <summary>Pencerede okunmamış bildirim sayısı.</summary>
        public int UnreadCount { get; set; }

        /// <summary>Tip bazında bildirim sayıları.</summary>
        public List<AdminNotificationTypeStatDto> ByType { get; set; } = new();

        /// <summary>
        /// Sağlayıcı bazlı gönderim maliyeti açıklaması. SMS/e-posta sağlayıcı
        /// entegrasyonu yapıldığında kanal bazlı maliyet kırılımı eklenecek.
        /// </summary>
        public string CostNote { get; set; } = string.Empty;
    }

    /// <summary>Tek bir bildirim tipinin penceredeki sayısı.</summary>
    public class AdminNotificationTypeStatDto
    {
        public EnumNotificationType Type { get; set; }

        /// <summary>Bu tipte gönderilen bildirim sayısı.</summary>
        public int Count { get; set; }
    }
}
