using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotificationStats
{
    /// <summary>
    /// Son 30 gunluk bildirim istatistikleri. Saglayici bazli gonderim maliyeti
    /// bu DTO'da henuz yer almaz; bkz. <see cref="CostNote"/>.
    /// </summary>
    public class AdminNotificationStatsDto
    {
        /// <summary>Istatistik penceresinin baslangic ani (UTC).</summary>
        public DateTime FromDate { get; set; }

        /// <summary>Istatistik penceresinin bitis ani (UTC).</summary>
        public DateTime ToDate { get; set; }

        /// <summary>Penceredeki toplam bildirim sayisi.</summary>
        public int Total { get; set; }

        /// <summary>Pencerede okunmus bildirim sayisi.</summary>
        public int ReadCount { get; set; }

        /// <summary>Pencerede okunmamis bildirim sayisi.</summary>
        public int UnreadCount { get; set; }

        /// <summary>Tip bazinda bildirim sayilari.</summary>
        public List<AdminNotificationTypeStatDto> ByType { get; set; } = new();

        /// <summary>
        /// Saglayici bazli gonderim maliyeti aciklamasi. SMS/e-posta saglayici
        /// entegrasyonu yapildiginda kanal bazli maliyet kirilimi eklenecek.
        /// </summary>
        public string CostNote { get; set; } = string.Empty;
    }

    /// <summary>Tek bir bildirim tipinin penceredeki sayisi.</summary>
    public class AdminNotificationTypeStatDto
    {
        public EnumNotificationType Type { get; set; }

        /// <summary>Bu tipte gonderilen bildirim sayisi.</summary>
        public int Count { get; set; }
    }
}
