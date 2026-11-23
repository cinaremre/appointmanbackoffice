using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Notifications.Commands.RetryAdminNotification
{
    /// <summary>
    /// Yeniden gönderim talebinin kabul sonucu. İlk sürümde asıl gönderim
    /// yapılmadığı için sonuç bir "kabul" bilgisidir; detay Note alanındadır.
    /// </summary>
    public class RetryAdminNotificationResultDto
    {
        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }
        public EnumNotificationType Type { get; set; }

        /// <summary>Kabul açıklaması; basitleştirmenin nedenini net biçimde belirtir.</summary>
        public string Note { get; set; } = string.Empty;
    }
}
