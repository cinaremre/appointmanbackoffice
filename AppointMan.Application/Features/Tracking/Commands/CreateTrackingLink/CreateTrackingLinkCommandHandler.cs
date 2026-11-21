using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Tracking.Commands.CreateTrackingLink
{
    public class CreateTrackingLinkCommandHandler : IRequestHandler<CreateTrackingLinkCommand, Result<string>>
    {
        private readonly IRepository<TrackingLink> _trackingLinkRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateTrackingLinkCommandHandler(
            IRepository<TrackingLink> trackingLinkRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _trackingLinkRepository = trackingLinkRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<string>> Handle(CreateTrackingLinkCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<string>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<string>.Failure(Error.Unauthorized("Bu isletme icin takip linki olusturma yetkiniz yok."));

                // 3. Benzersiz kisa kod uret. Guid tabanli oldugu icin pratikte
                //    cakisma imkansizdir; kodun kendisi linkin kimligidir.
                //    QR payload'i frontend bu kod ile kurar.
                var trackingLink = new TrackingLink(
                    request.BusinessId,
                    request.TargetType,
                    request.Source,
                    request.DestinationPath,
                    request.TargetId,
                    request.Medium,
                    request.Campaign);

                // 4. Kaydet
                await _trackingLinkRepository.AddAsync(trackingLink, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<string>.Success(trackingLink.TrackingCode);
            }
            catch (DomainException ex)
            {
                return Result<string>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(Error.Internal("Takip linki olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}
