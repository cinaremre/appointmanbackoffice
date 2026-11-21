using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Subscriptions.Queries.GetAdminSubscriptionPlans
{
    /// <summary>
    /// Planlari ve ozelliklerini birlestirip dondurur. Ilk surumde uc
    /// koleksiyon tamamen belleke cekilip eslenir; plan sayisi dusuk
    /// oldugundan (dokuman 13) bu kabul edilebilir.
    /// </summary>
    public class GetAdminSubscriptionPlansQueryHandler
        : IRequestHandler<GetAdminSubscriptionPlansQuery, Result<IReadOnlyList<AdminSubscriptionPlanDto>>>
    {
        private readonly IRepository<SubscriptionPlan> _planRepository;
        private readonly IRepository<PlanFeature> _planFeatureRepository;
        private readonly IRepository<FeatureDefinition> _featureDefinitionRepository;

        public GetAdminSubscriptionPlansQueryHandler(
            IRepository<SubscriptionPlan> planRepository,
            IRepository<PlanFeature> planFeatureRepository,
            IRepository<FeatureDefinition> featureDefinitionRepository)
        {
            _planRepository = planRepository;
            _planFeatureRepository = planFeatureRepository;
            _featureDefinitionRepository = featureDefinitionRepository;
        }

        public async Task<Result<IReadOnlyList<AdminSubscriptionPlanDto>>> Handle(
            GetAdminSubscriptionPlansQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Planlar, plan-ozellik kayitlari ve ozellik tanimlari.
                var plans = await _planRepository.FindAsync(p => !p.IsDeleted, cancellationToken);
                var planFeatures = await _planFeatureRepository.FindAsync(f => !f.IsDeleted, cancellationToken);
                var definitions = await _featureDefinitionRepository.FindAsync(d => !d.IsDeleted, cancellationToken);

                var definitionById = definitions.ToDictionary(d => d.Id);

                // 2. Plan bazinda ozellik eslesmesi; tanimi silinmis satirlar atlanir.
                var result = plans
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .Select(plan => new AdminSubscriptionPlanDto
                    {
                        Id = plan.Id,
                        Name = plan.Name,
                        Price = plan.Price,
                        Currency = plan.Currency,
                        BillingPeriod = plan.BillingPeriod,
                        IsActive = plan.IsActive,
                        Features = planFeatures
                            .Where(pf => pf.PlanId == plan.Id)
                            .Where(pf => definitionById.ContainsKey(pf.FeatureId))
                            .Select(pf => new AdminPlanFeatureDto
                            {
                                FeatureCode = definitionById[pf.FeatureId].Code,
                                IsEnabled = pf.IsEnabled,
                                IncludedQuota = pf.IncludedQuota
                            })
                            .ToList()
                    })
                    .ToList();

                return Result<IReadOnlyList<AdminSubscriptionPlanDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<AdminSubscriptionPlanDto>>.Failure(
                    Error.Internal("Planlar getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
