using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotifications
{
    /// <summary>
    /// Platform bildirim listesi (doküman Modül 14). Kullanıcı, tip ve gönderim
    /// zamanı filtreleriyle sayfalı bildirim listesini getirir.
    /// </summary>
    public class GetAdminNotificationsQuery : IRequest<Result<AdminNotificationListDto>>
    {
        /// <summary>Yalnızca bu kullanıcıya gönderilen bildirimler listelenir.</summary>
        public Guid? UserId { get; set; }

        /// <summary>Bildirim tipi filtresi (EnumNotificationType).</summary>
        public EnumNotificationType? Type { get; set; }

        /// <summary>Gönderim zamanı alt sınırı (SentAt >= FromDate).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Gönderim zamanı üst sınırı (SentAt &lt;= ToDate).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanlı sayfa numarası; kırpanma kuralı AdminPagination'dadır.</summary>
        public int Page { get; set; }

        /// <summary>Sayfa boyutu; en fazla 200, varsayılan 50.</summary>
        public int PageSize { get; set; }
    }
}
