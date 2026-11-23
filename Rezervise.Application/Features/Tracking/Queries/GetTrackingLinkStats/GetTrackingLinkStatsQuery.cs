using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Tracking.Queries.GetTrackingLinkStats
{
    /// <summary>
    /// İşletmenin takip linkleri için ölçüm özeti:
    /// toplam açılış, benzersiz oturum sayısı ve olay tipi kırılımı.
    /// </summary>
    public class GetTrackingLinkStatsQuery : IRequest<Result<List<TrackingLinkStatsDto>>>
    {
        public Guid BusinessId { get; set; }
    }
}
