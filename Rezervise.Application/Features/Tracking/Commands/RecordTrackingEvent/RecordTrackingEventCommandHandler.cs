using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Tracking.Commands.RecordTrackingEvent
{
    /// <summary>
    /// Anonim takip olayi kaydeder. ANONIM uctur: handler
    /// ICurrentUserService'e hic baglanmaz.
    /// Kod/slug bulunamazsa sessiz basari doner (Result.Success(true));
    /// linkin ya da sayfanin var olup olmadigi hakkinda bilgi sizdirilmaz.
    /// NOT: link suresi dolmus / randevu iptal kontrolu bu surumde yok.
    /// </summary>
    public class RecordTrackingEventCommandHandler : IRequestHandler<RecordTrackingEventCommand, Result<bool>>
    {
        private readonly IRepository<TrackingLink> _trackingLinkRepository;
        private readonly IRepository<BusinessPublicPage> _pageRepository;
        private readonly IRepository<TrackingEvent> _trackingEventRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RecordTrackingEventCommandHandler(
            IRepository<TrackingLink> trackingLinkRepository,
            IRepository<BusinessPublicPage> pageRepository,
            IRepository<TrackingEvent> trackingEventRepository,
            IUnitOfWork unitOfWork)
        {
            _trackingLinkRepository = trackingLinkRepository;
            _pageRepository = pageRepository;
            _trackingEventRepository = trackingEventRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(RecordTrackingEventCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Takip kodu verildiyse link uzerinden kayit yap.
                if (!string.IsNullOrWhiteSpace(request.TrackingCode))
                {
                    var code = request.TrackingCode.Trim().ToLowerInvariant();

                    var link = (await _trackingLinkRepository.FindAsync(
                        l => l.TrackingCode == code && l.IsActive && !l.IsDeleted,
                        cancellationToken)).FirstOrDefault();

                    // Kod bulunamadiysa sessiz basari: hedefin varligi hakkinda bilgi vermez.
                    if (link is null)
                        return Result<bool>.Success(true);

                    var linkEvent = new TrackingEvent(
                        link.BusinessId,
                        request.EventType,
                        link.Id,
                        request.AnonymousSessionId,
                        request.AppointmentId);

                    await _trackingEventRepository.AddAsync(linkEvent, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<bool>.Success(true);
                }

                // 2. Slug verildiyse public sayfa uzerinden kayit yap.
                if (!string.IsNullOrWhiteSpace(request.Slug))
                {
                    var slug = request.Slug.Trim().ToLowerInvariant();

                    var page = (await _pageRepository.FindAsync(
                        p => p.Slug == slug && p.IsPublished && !p.IsDeleted,
                        cancellationToken)).FirstOrDefault();

                    // Sayfa bulunamadiysa sessiz basari.
                    if (page is null)
                        return Result<bool>.Success(true);

                    var pageEvent = new TrackingEvent(
                        page.BusinessId,
                        request.EventType,
                        trackingLinkId: null,
                        request.AnonymousSessionId,
                        request.AppointmentId);

                    await _trackingEventRepository.AddAsync(pageEvent, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<bool>.Success(true);
                }

                // 3. Ne kod ne slug verildi: istek hatali bicimlendirilmis.
                return Result<bool>.Failure(Error.Validation("Takip kodu veya slug gereklidir."));
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(Error.Internal("Takip olayi kaydedilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
