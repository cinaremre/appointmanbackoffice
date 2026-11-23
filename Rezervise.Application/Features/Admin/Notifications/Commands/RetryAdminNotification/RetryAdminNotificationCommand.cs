using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Notifications.Commands.RetryAdminNotification
{
    /// <summary>
    /// Bildirim yeniden gönderim talebi (doküman Modül 14, kritik işlem).
    /// Denetim kaydı zorunludur; handler Action="NotificationRetry" yazar.
    /// </summary>
    public class RetryAdminNotificationCommand : IRequest<Result<RetryAdminNotificationResultDto>>
    {
        /// <summary>Yeniden gönderimi istenen bildirimin kimliği.</summary>
        public Guid NotificationId { get; set; }

        /// <summary>
        /// İşlemi yapan yöneticinin User ID'si. Controller bu alani JWT
        /// claim'lerinden doldurur; handler istemciye güvenmez.
        /// </summary>
        public Guid ActorUserId { get; set; }

        /// <summary>İşlem anındaki yönetici rol adı (denetim kaydı için).</summary>
        public string ActorRole { get; set; } = string.Empty;

        /// <summary>Yöneticinin işlem gerekçesi; opsiyonel.</summary>
        public string? Reason { get; set; }
    }
}
