using AppointMan.Domain.Enums;

namespace AppointMan.Application.Features.Admin.Notifications.Commands.RetryAdminNotification
{
    /// <summary>
    /// Yeniden gonderim talebinin kabul sonucu. Ilk surumde asil gonderim
    /// yapilmadigi icin sonuc bir "kabul" bilgisidir; detay Note alanindadir.
    /// </summary>
    public class RetryAdminNotificationResultDto
    {
        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }
        public EnumNotificationType Type { get; set; }

        /// <summary>Kabul aciklamasi; basitlestirmenin nedenini net bicimde belirtir.</summary>
        public string Note { get; set; } = string.Empty;
    }
}
