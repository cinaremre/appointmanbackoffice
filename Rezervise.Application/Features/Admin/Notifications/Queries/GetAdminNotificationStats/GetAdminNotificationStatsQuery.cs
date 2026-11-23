using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotificationStats
{
    /// <summary>
    /// Son 30 günün bildirim istatistikleri (doküman Modül 14): toplam, okunmuş,
    /// okunmamış sayıları ve tip bazında kırılım.
    /// </summary>
    public class GetAdminNotificationStatsQuery : IRequest<Result<AdminNotificationStatsDto>>
    {
    }
}
