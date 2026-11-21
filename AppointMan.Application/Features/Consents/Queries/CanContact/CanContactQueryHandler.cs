using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Consents.Queries.CanContact
{
    public class CanContactQueryHandler : IRequestHandler<CanContactQuery, Result<bool>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<CustomerCommunicationPreference> _preferenceRepository;
        private readonly IRepository<CommunicationSuppression> _suppressionRepository;
        private readonly ICurrentUserService _currentUser;

        public CanContactQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<CustomerCommunicationPreference> preferenceRepository,
            IRepository<CommunicationSuppression> suppressionRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _preferenceRepository = preferenceRepository;
            _suppressionRepository = suppressionRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<bool>> Handle(CanContactQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<bool>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki: gonderim karari isletme sahibi veya platform yoneticisine aittir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<bool>.Failure(Error.Unauthorized("Bu isletmenin iletisim izinlerini sorgulama yetkiniz yok."));

                // 3. Kanal tamamen kapali mi? Suppression amac fark etmeksizin gecerlidir.
                var suppressed = await _suppressionRepository.ExistsAsync(
                    s => s.BusinessId == request.BusinessId
                        && s.CustomerId == request.CustomerId
                        && s.Channel == request.Channel
                        && !s.IsDeleted,
                    cancellationToken);

                if (suppressed)
                    return Result<bool>.Success(false);

                // 4. Kayitli tercih varsa onu esas al.
                var preference = (await _preferenceRepository.FindAsync(
                    p => p.BusinessId == request.BusinessId
                        && p.CustomerId == request.CustomerId
                        && p.Channel == request.Channel
                        && p.Purpose == request.Purpose
                        && !p.IsDeleted,
                    cancellationToken)).FirstOrDefault();

                if (preference is not null)
                    return Result<bool>.Success(preference.IsAllowed);

                // 5. Tercih yoksa amac bazli varsayilan (dokuman Modul 16):
                //    islemsel mesajlar (randevu bildirimi vb.) onay varsayilir,
                //    pazarlama mesajlari icin acik riza beklenir.
                var isAllowedByDefault = request.Purpose == EnumConsentPurpose.Transactional;
                return Result<bool>.Success(isAllowedByDefault);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(Error.Internal("Iletisim izni sorgulanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
