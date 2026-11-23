using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Queries.GetBusinessSubscription
{
    public class GetBusinessSubscriptionQueryHandler
        : IRequestHandler<GetBusinessSubscriptionQuery, Result<AdminBusinessSubscriptionDto?>>
    {
        private readonly IRepository<BusinessSubscription> _subscriptionRepository;
        private readonly IRepository<SubscriptionPlan> _planRepository;
        private readonly IRepository<PlanFeature> _planFeatureRepository;
        private readonly IRepository<FeatureDefinition> _featureDefinitionRepository;

        public GetBusinessSubscriptionQueryHandler(
            IRepository<BusinessSubscription> subscriptionRepository,
            IRepository<SubscriptionPlan> planRepository,
            IRepository<PlanFeature> planFeatureRepository,
            IRepository<FeatureDefinition> featureDefinitionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
            _planRepository = planRepository;
            _planFeatureRepository = planFeatureRepository;
            _featureDefinitionRepository = featureDefinitionRepository;
        }

        public async Task<Result<AdminBusinessSubscriptionDto?>> Handle(
            GetBusinessSubscriptionQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletmenin silinmemiş abonelikleri.
                var subscriptions = await _subscriptionRepository.FindAsync(
                    s => s.BusinessId == request.BusinessId && !s.IsDeleted,
                    cancellationToken);

                var now = DateTime.UtcNow;

                // 2. Geçerli abonelik: önce tarih penceresi içinde aktif olan,
                //    yoksa ileride başlayacak en son atanan kayıt.
                var subscription = subscriptions
                    .Where(s => s.IsActiveAt(now))
                    .OrderByDescending(s => s.StartAt)
                    .FirstOrDefault()
                    ?? subscriptions
                        .Where(s => s.Status == EnumSubscriptionStatus.Active)
                        .OrderByDescending(s => s.StartAt)
                        .FirstOrDefault();

                // 3. Abonelik yoksa boş başarı: bu işletme için normal durum.
                if (subscription is null)
                    return Result<AdminBusinessSubscriptionDto?>.Success(null);

                // 4. Plan bilgisi ve özellik listesi.
                var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
                var planFeatures = await _planFeatureRepository.FindAsync(
                    pf => pf.PlanId == subscription.PlanId && !pf.IsDeleted,
                    cancellationToken);
                var definitions = await _featureDefinitionRepository.FindAsync(
                    d => !d.IsDeleted,
                    cancellationToken);

                var definitionById = definitions.ToDictionary(d => d.Id);

                var dto = new AdminBusinessSubscriptionDto
                {
                    SubscriptionId = subscription.Id,
                    PlanId = subscription.PlanId,
                    PlanName = plan?.Name ?? string.Empty,
                    StartAt = subscription.StartAt,
                    EndAt = subscription.EndAt,
                    Status = subscription.Status,
                    PlanFeatures = planFeatures
                        .Where(pf => definitionById.ContainsKey(pf.FeatureId))
                        .Select(pf => new AdminSubscriptionPlanFeatureDto
                        {
                            FeatureCode = definitionById[pf.FeatureId].Code,
                            IsEnabled = pf.IsEnabled,
                            IncludedQuota = pf.IncludedQuota
                        })
                        .ToList()
                };

                return Result<AdminBusinessSubscriptionDto?>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<AdminBusinessSubscriptionDto?>.Failure(
                    Error.Internal("İşletme aboneliği getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
