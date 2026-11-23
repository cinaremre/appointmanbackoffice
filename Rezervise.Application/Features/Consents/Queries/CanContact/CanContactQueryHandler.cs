using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Consents.Queries.CanContact
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
                // 1. İşletme kontrolü.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<bool>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki: gönderim kararı işletme sahibi veya platform yöneticisine aittir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<bool>.Failure(Error.Unauthorized("Bu işletmenin iletişim izinlerini sorgulama yetkiniz yok."));

                // 3. Kanal tamamen kapalı mi? Suppression amaç fark etmeksizin geçerlidir.
                var suppressed = await _suppressionRepository.ExistsAsync(
                    s => s.BusinessId == request.BusinessId
                        && s.CustomerId == request.CustomerId
                        && s.Channel == request.Channel
                        && !s.IsDeleted,
                    cancellationToken);

                if (suppressed)
                    return Result<bool>.Success(false);

                // 4. Kayıtlı tercih varsa onu esas al.
                var preference = (await _preferenceRepository.FindAsync(
                    p => p.BusinessId == request.BusinessId
                        && p.CustomerId == request.CustomerId
                        && p.Channel == request.Channel
                        && p.Purpose == request.Purpose
                        && !p.IsDeleted,
                    cancellationToken)).FirstOrDefault();

                if (preference is not null)
                    return Result<bool>.Success(preference.IsAllowed);

                // 5. Tercih yoksa amaç bazlı varsayılan (doküman Modül 16):
                //    işlemsel mesajlar (randevu bildirimi vb.) onay varsayılır,
                //    pazarlama mesajları için açık rıza beklenir.
                var isAllowedByDefault = request.Purpose == EnumConsentPurpose.Transactional;
                return Result<bool>.Success(isAllowedByDefault);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(Error.Internal("İletişim izni sorgulanırken bir hata oluştu.", ex.Message));
            }
        }
    }
}
