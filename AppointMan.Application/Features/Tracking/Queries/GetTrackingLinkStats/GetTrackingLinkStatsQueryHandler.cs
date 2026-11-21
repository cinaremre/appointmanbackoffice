using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Tracking.Queries.GetTrackingLinkStats
{
    public class GetTrackingLinkStatsQueryHandler : IRequestHandler<GetTrackingLinkStatsQuery, Result<List<TrackingLinkStatsDto>>>
    {
        private readonly IRepository<TrackingLink> _trackingLinkRepository;
        private readonly IRepository<TrackingEvent> _trackingEventRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly ICurrentUserService _currentUser;

        public GetTrackingLinkStatsQueryHandler(
            IRepository<TrackingLink> trackingLinkRepository,
            IRepository<TrackingEvent> trackingEventRepository,
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser)
        {
            _trackingLinkRepository = trackingLinkRepository;
            _trackingEventRepository = trackingEventRepository;
            _businessRepository = businessRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<List<TrackingLinkStatsDto>>> Handle(GetTrackingLinkStatsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<List<TrackingLinkStatsDto>>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: olcum verisi isletme sahibine ozeldir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<List<TrackingLinkStatsDto>>.Failure(Error.Unauthorized("Bu isletmenin takip istatistiklerini goruntuleme yetkiniz yok."));

                // 3. Linkler ve olaylar (olaylar link basinda gruplanacak)
                var links = await _trackingLinkRepository.FindAsync(
                    l => l.BusinessId == request.BusinessId && !l.IsDeleted,
                    cancellationToken);

                var events = await _trackingEventRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId && !e.IsDeleted,
                    cancellationToken);

                // 4. Link basina toplula: acilis sayisi, benzersiz oturum, olay kirilimi.
                var dtos = links
                    .OrderBy(l => l.CreatedAt)
                    .Select(l =>
                    {
                        var linkEvents = events.Where(e => e.TrackingLinkId == l.Id).ToList();

                        return new TrackingLinkStatsDto
                        {
                            LinkId = l.Id,
                            TrackingCode = l.TrackingCode,
                            Source = l.Source,
                            DestinationPath = l.DestinationPath,
                            TotalOpens = linkEvents.Count(e => e.EventType == EnumTrackingEventType.LinkOpened),
                            UniqueSessions = linkEvents
                                .Where(e => e.AnonymousSessionId.HasValue)
                                .Select(e => e.AnonymousSessionId!.Value)
                                .Distinct()
                                .Count(),
                            EventCounts = linkEvents
                                .GroupBy(e => e.EventType)
                                .Select(g => new TrackingEventCountDto { EventType = g.Key, Count = g.Count() })
                                .OrderBy(c => c.EventType)
                                .ToList()
                        };
                    })
                    .ToList();

                return Result<List<TrackingLinkStatsDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<TrackingLinkStatsDto>>.Failure(
                    Error.Internal("Takip istatistikleri getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
