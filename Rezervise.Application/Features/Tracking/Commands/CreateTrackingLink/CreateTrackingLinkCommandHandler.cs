using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Tracking.Commands.CreateTrackingLink
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
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<string>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<string>.Failure(Error.Unauthorized("Bu işletme için takip linki oluşturma yetkiniz yok."));

                // 3. Benzersiz kısa kod üret. Guid tabanlı öldüğü için pratikte
                //    çakışma imkansızdır; kodun kendisi linkin kimliğidir.
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
                return Result<string>.Failure(Error.Internal("Takip linki oluşturulurken bir hata oluştu.", ex.Message));
            }
        }
    }
}
