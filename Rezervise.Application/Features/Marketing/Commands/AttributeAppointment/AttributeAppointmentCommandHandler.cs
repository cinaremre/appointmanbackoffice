using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Marketing.Commands.AttributeAppointment
{
    public class AttributeAppointmentCommandHandler : IRequestHandler<AttributeAppointmentCommand, Result<Guid>>
    {
        private readonly IRepository<AppointmentAttribution> _attributionRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<MarketingCampaign> _campaignRepository;
        private readonly IRepository<TrackingLink> _trackingLinkRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public AttributeAppointmentCommandHandler(
            IRepository<AppointmentAttribution> attributionRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<MarketingCampaign> campaignRepository,
            IRepository<TrackingLink> trackingLinkRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _attributionRepository = attributionRepository;
            _appointmentRepository = appointmentRepository;
            _campaignRepository = campaignRepository;
            _trackingLinkRepository = trackingLinkRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(AttributeAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu işletmenin randevusuna atıf girme yetkiniz yok."));

                // 3. Randevu bu işletmeye ait olmalı.
                var appointmentBelongsToBusiness = await _appointmentRepository.ExistsAsync(
                    a => a.Id == request.AppointmentId && a.BusinessId == request.BusinessId && !a.IsDeleted,
                    cancellationToken);

                if (!appointmentBelongsToBusiness)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // 4. DuplicateKey koruması: aynı randevuya ikinci atıf Conflict.
                var alreadyAttributed = await _attributionRepository.ExistsAsync(
                    x => x.AppointmentId == request.AppointmentId && !x.IsDeleted,
                    cancellationToken);

                if (alreadyAttributed)
                    return Result<Guid>.Failure(Error.Conflict("Bu randevu için zaten bir atıf kaydı mevcut."));

                // 5. Verilen kampanya / takip linki aynı işletmeye ait olmalı.
                if (request.CampaignId.HasValue)
                {
                    var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId.Value, cancellationToken);
                    if (campaign is null || campaign.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.NotFound("Kampanya", request.CampaignId.Value.ToString()));
                }

                if (request.TrackingLinkId.HasValue)
                {
                    var link = await _trackingLinkRepository.GetByIdAsync(request.TrackingLinkId.Value, cancellationToken);
                    if (link is null || link.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.NotFound("Takip linki", request.TrackingLinkId.Value.ToString()));
                }

                // 6. Atıf kaydını oluştur
                var attribution = new AppointmentAttribution(
                    request.AppointmentId,
                    request.BusinessId,
                    request.AttributionModel,
                    request.CampaignId,
                    request.TrackingLinkId,
                    request.FirstTouchSource,
                    request.LastTouchSource);

                // 7. Kaydet
                await _attributionRepository.AddAsync(attribution, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(attribution.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Randevu atıfı kaydedilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
