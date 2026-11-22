using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Tracking.Queries.GetTrackingLinkStats
{
    /// <summary>
    /// Isletmenin takip linkleri icin olcum ozeti:
    /// toplam acilis, benzersiz oturum sayisi ve olay tipi kirilimi.
    /// </summary>
    public class GetTrackingLinkStatsQuery : IRequest<Result<List<TrackingLinkStatsDto>>>
    {
        public Guid BusinessId { get; set; }
    }
}
