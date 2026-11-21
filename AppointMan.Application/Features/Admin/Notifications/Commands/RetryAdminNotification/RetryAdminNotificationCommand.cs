using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Notifications.Commands.RetryAdminNotification
{
    /// <summary>
    /// Bildirim yeniden gonderim talebi (dokuman Modul 14, kritik islem).
    /// Denetim kaydi zorunludur; handler Action="NotificationRetry" yazar.
    /// </summary>
    public class RetryAdminNotificationCommand : IRequest<Result<RetryAdminNotificationResultDto>>
    {
        /// <summary>Yeniden gonderimi istenen bildirimin kimligi.</summary>
        public Guid NotificationId { get; set; }

        /// <summary>
        /// Islemi yapan yoneticinin User ID'si. Controller bu alani JWT
        /// claim'lerinden doldurur; handler istemciye guvenmez.
        /// </summary>
        public Guid ActorUserId { get; set; }

        /// <summary>Islem anindaki yonetici rol adi (denetim kaydi icin).</summary>
        public string ActorRole { get; set; } = string.Empty;

        /// <summary>Yoneticinin islem gerekcesi; opsiyonel.</summary>
        public string? Reason { get; set; }
    }
}
