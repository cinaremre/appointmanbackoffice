using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Subscriptions.Commands.CreatePlan
{
    public class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, Result<Guid>>
    {
        private readonly IRepository<SubscriptionPlan> _planRepository;
        private readonly IRepository<PlanFeature> _planFeatureRepository;
        private readonly IRepository<FeatureDefinition> _featureDefinitionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IWorkflowAuditSink _auditSink;

        public CreatePlanCommandHandler(
            IRepository<SubscriptionPlan> planRepository,
            IRepository<PlanFeature> planFeatureRepository,
            IRepository<FeatureDefinition> featureDefinitionRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IWorkflowAuditSink auditSink)
        {
            _planRepository = planRepository;
            _planFeatureRepository = planFeatureRepository;
            _featureDefinitionRepository = featureDefinitionRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
        }

        public async Task<Result<Guid>> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yetki: plan tanimlamak platform yoneticisi isidir.
                if (!CallerAuthorization.IsAdministrator(_currentUser))
                    return Result<Guid>.Failure(Error.Forbidden("Plan olusturma yalniz platform yoneticileri icindir."));

                // 2. Ozellik tanimlari dogrulanir; olmayan ozellik plana eklenemez.
                var featureIds = request.Features.Select(f => f.FeatureId).Distinct().ToList();
                var definitions = await _featureDefinitionRepository.FindAsync(
                    d => featureIds.Contains(d.Id) && !d.IsDeleted,
                    cancellationToken);

                var missing = featureIds.Except(definitions.Select(d => d.Id)).ToList();
                if (missing.Count > 0)
                {
                    return Result<Guid>.Failure(Error.Validation(
                        "Bazi ozellik tanimlari bulunamadi.",
                        $"Eksik ozellikler: {string.Join(", ", missing)}"));
                }

                // 3. Plan olustur.
                var plan = new SubscriptionPlan(
                    request.Name,
                    request.Price,
                    request.Currency,
                    request.BillingPeriod);

                await _planRepository.AddAsync(plan, cancellationToken);

                // 4. Plan ozellikleri olustur.
                foreach (var feature in request.Features)
                {
                    var planFeature = new PlanFeature(
                        plan.Id,
                        feature.FeatureId,
                        feature.IsEnabled,
                        feature.IncludedQuota);

                    await _planFeatureRepository.AddAsync(planFeature, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 5. Denetim kaydi.
                _auditSink.Record(
                    "Subscription.PlanCreated",
                    Guid.NewGuid(),
                    plan.Id,
                    $"Name={plan.Name}; Price={plan.Price} {plan.Currency}; Features={request.Features.Count}");

                return Result<Guid>.Success(plan.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Plan olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}
