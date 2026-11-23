using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Subscriptions.Commands.GrantFeatureOverride
{
    public class GrantFeatureOverrideCommandHandler : IRequestHandler<GrantFeatureOverrideCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<FeatureDefinition> _featureDefinitionRepository;
        private readonly IRepository<BusinessFeatureOverride> _overrideRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IWorkflowAuditSink _auditSink;

        public GrantFeatureOverrideCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<FeatureDefinition> featureDefinitionRepository,
            IRepository<BusinessFeatureOverride> overrideRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IWorkflowAuditSink auditSink)
        {
            _businessRepository = businessRepository;
            _featureDefinitionRepository = featureDefinitionRepository;
            _overrideRepository = overrideRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
        }

        public async Task<Result<Guid>> Handle(GrantFeatureOverrideCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yetki: erişim kararı platform yöneticisi işidir.
                if (!CallerAuthorization.IsAdministrator(_currentUser))
                    return Result<Guid>.Failure(Error.Forbidden("Özellik override yalnız platform yöneticileri içindir."));

                // 2. İşletme kontrolü.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 3. Özellik tanımı kontrolü.
                var feature = await _featureDefinitionRepository.GetByIdAsync(request.FeatureId, cancellationToken);
                if (feature is null)
                    return Result<Guid>.Failure(Error.NotFound("Özellik", request.FeatureId.ToString()));

                // 4. Override kaydını oluştur. GrantedBy gövdeden değil JWT kimliğinden alınır.
                var overrideRecord = new BusinessFeatureOverride(
                    request.BusinessId,
                    request.FeatureId,
                    request.IsEnabled,
                    request.QuotaOverride,
                    request.ValidFrom,
                    request.ValidUntil,
                    request.Reason,
                    _currentUser.UserId!.Value);

                await _overrideRepository.AddAsync(overrideRecord, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 5. Denetim kaydı: kim, hangi işletmeye, hangi özelliği ne zaman verdi.
                _auditSink.Record(
                    "Subscription.FeatureOverrideGranted",
                    Guid.NewGuid(),
                    request.BusinessId,
                    $"Feature={feature.Code}; IsEnabled={request.IsEnabled}; Quota={request.QuotaOverride?.ToString() ?? "plan"}; "
                    + $"ValidFrom={request.ValidFrom:yyyy-MM-dd}; ValidUntil={request.ValidUntil?.ToString("yyyy-MM-dd") ?? "belirsiz"}; "
                    + $"GrantedBy={_currentUser.UserId}; Reason={request.Reason ?? "-"}");

                return Result<Guid>.Success(overrideRecord.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Özellik override verilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
