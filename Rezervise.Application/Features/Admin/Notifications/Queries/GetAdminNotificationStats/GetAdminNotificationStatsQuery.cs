using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotificationStats
{
    /// <summary>
    /// Son 30 gunun bildirim istatistikleri (dokuman Modul 14): toplam, okunmus,
    /// okunmamis sayilari ve tip bazinda kirilim.
    /// </summary>
    public class GetAdminNotificationStatsQuery : IRequest<Result<AdminNotificationStatsDto>>
    {
    }
}
