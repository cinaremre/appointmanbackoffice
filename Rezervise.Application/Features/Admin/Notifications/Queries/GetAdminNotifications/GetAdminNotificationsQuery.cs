using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotifications
{
    /// <summary>
    /// Platform bildirim listesi (dokuman Modul 14). Kullanici, tip ve gonderim
    /// zamani filtreleriyle sayfali bildirim listesini getirir.
    /// </summary>
    public class GetAdminNotificationsQuery : IRequest<Result<AdminNotificationListDto>>
    {
        /// <summary>Yalnizca bu kullaniciya gonderilen bildirimler listelenir.</summary>
        public Guid? UserId { get; set; }

        /// <summary>Bildirim tipi filtresi (EnumNotificationType).</summary>
        public EnumNotificationType? Type { get; set; }

        /// <summary>Gonderim zamani alt siniri (SentAt >= FromDate).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Gonderim zamani ust siniri (SentAt &lt;= ToDate).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanli sayfa numarasi; kirpanma kurali AdminPagination'dadir.</summary>
        public int Page { get; set; }

        /// <summary>Sayfa boyutu; en fazla 200, varsayilan 50.</summary>
        public int PageSize { get; set; }
    }
}
