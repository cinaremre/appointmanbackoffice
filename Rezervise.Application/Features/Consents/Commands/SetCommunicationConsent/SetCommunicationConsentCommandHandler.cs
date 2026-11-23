using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Consents.Commands.SetCommunicationConsent
{
    public class SetCommunicationConsentCommandHandler : IRequestHandler<SetCommunicationConsentCommand, Result<Guid>>
    {
        /// <summary>İzin değişikliğinin kabul edilebilir kaynakları.</summary>
        private static readonly string[] AllowedSources = { "api", "import" };

        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<CustomerCommunicationPreference> _preferenceRepository;
        private readonly IRepository<ConsentHistory> _consentHistoryRepository;
        private readonly IRepository<CommunicationSuppression> _suppressionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public SetCommunicationConsentCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<CustomerCommunicationPreference> preferenceRepository,
            IRepository<ConsentHistory> consentHistoryRepository,
            IRepository<CommunicationSuppression> suppressionRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _preferenceRepository = preferenceRepository;
            _consentHistoryRepository = consentHistoryRepository;
            _suppressionRepository = suppressionRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(SetCommunicationConsentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki: izin kaydı işletme sahibi veya platform yöneticisi tarafından yapılır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu işletmenin iletişim izinlerini düzenleme yetkiniz yok."));

                // 3. Kaynak doğrulaması.
                if (string.IsNullOrWhiteSpace(request.Source)
                    || !AllowedSources.Contains(request.Source.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    return Result<Guid>.Failure(Error.Validation("İzin kaynağı 'api' veya 'import' olmalı."));
                }

                // 4. Preference upsert: (işletme, müşteri, kanal, amaç) için tek kayıt.
                var preference = (await _preferenceRepository.FindAsync(
                    p => p.BusinessId == request.BusinessId
                        && p.CustomerId == request.CustomerId
                        && p.Channel == request.Channel
                        && p.Purpose == request.Purpose
                        && !p.IsDeleted,
                    cancellationToken)).FirstOrDefault();

                if (preference is null)
                {
                    preference = new CustomerCommunicationPreference(
                        request.BusinessId,
                        request.CustomerId,
                        request.Channel,
                        request.Purpose,
                        request.IsAllowed,
                        evidenceReference: request.EvidenceReference);

                    await _preferenceRepository.AddAsync(preference, cancellationToken);
                }
                else
                {
                    // Aynı değer atanırsa Set hiçbir değişiklik ve event üretmez.
                    preference.Set(request.IsAllowed, evidenceReference: request.EvidenceReference);
                    await _preferenceRepository.UpdateAsync(preference, cancellationToken);
                }

                // 5. Append-only geçmiş kaydı.
                var history = new ConsentHistory(
                    request.BusinessId,
                    request.CustomerId,
                    request.Channel,
                    request.Purpose,
                    request.IsAllowed ? EnumConsentAction.Granted : EnumConsentAction.Withdrawn,
                    request.Source,
                    request.EvidenceReference);

                await _consentHistoryRepository.AddAsync(history, cancellationToken);

                // 6. Pazarlama izni geri çekildiyse kanalı tamamen kapat (suppression).
                //    İzin verildiyse kanal açılsın diye mevcut suppression kaldırılır.
                var suppression = (await _suppressionRepository.FindAsync(
                    s => s.BusinessId == request.BusinessId
                        && s.CustomerId == request.CustomerId
                        && s.Channel == request.Channel
                        && !s.IsDeleted,
                    cancellationToken)).FirstOrDefault();

                if (!request.IsAllowed && request.Purpose == EnumConsentPurpose.Marketing)
                {
                    if (suppression is null)
                    {
                        var newSuppression = new CommunicationSuppression(
                            request.BusinessId,
                            request.CustomerId,
                            request.Channel,
                            "Pazarlama izni geri çekildi.");

                        await _suppressionRepository.AddAsync(newSuppression, cancellationToken);
                    }
                }
                else if (request.IsAllowed && suppression is not null)
                {
                    await _suppressionRepository.DeleteAsync(suppression, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(preference.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("İletişim izni kaydedilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
