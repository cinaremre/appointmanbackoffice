using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Reports.Queries.GetSubscriptionRevenue
{
    /// <summary>
    /// Abonelik gelir handler'i (doküman Modül 18). Aktif abonelikler
    /// BusinessSubscription.IsActiveAt semantiğiyle (Active durum + geçerlilik
    /// aralığı) seçilir; plan bilgileri ayrı repository'den eşleştirilir.
    /// </summary>
    public class GetSubscriptionRevenueQueryHandler : IRequestHandler<GetSubscriptionRevenueQuery, Result<SubscriptionRevenueDto>>
    {
        private readonly IRepository<BusinessSubscription> _subscriptionRepository;
        private readonly IRepository<SubscriptionPlan> _planRepository;

        public GetSubscriptionRevenueQueryHandler(
            IRepository<BusinessSubscription> subscriptionRepository,
            IRepository<SubscriptionPlan> planRepository)
        {
            _subscriptionRepository = subscriptionRepository;
            _planRepository = planRepository;
        }

        public async Task<Result<SubscriptionRevenueDto>> Handle(GetSubscriptionRevenueQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Su anda aktif abonelikler.
                var now = DateTime.UtcNow;

                var subscriptions = await _subscriptionRepository.FindAsync(
                    s => s.Status == EnumSubscriptionStatus.Active
                        && !s.IsDeleted
                        && (s.EndAt == null || s.EndAt > now),
                    cancellationToken);

                // 2. Plan bilgileri kod eşleştirmesi.
                var plans = await _planRepository.GetAllAsync(cancellationToken);
                var planById = plans.ToDictionary(p => p.Id);

                // 3. Plan bazında dağılım.
                var planDistribution = subscriptions
                    .GroupBy(s => s.PlanId)
                    .Select(g =>
                    {
                        planById.TryGetValue(g.Key, out var plan);

                        return new PlanDistributionDto
                        {
                            PlanId = g.Key,
                            PlanName = plan?.Name ?? "Bilinmeyen Plan",
                            BusinessCount = g.Count()
                        };
                    })
                    .OrderByDescending(p => p.BusinessCount)
                    .ThenBy(p => p.PlanName)
                    .ToList();

                // 4. MRR: Monthly tam fiyat, Yearly fiyatın 1/12'si.
                var mrr = subscriptions.Sum(s =>
                {
                    if (!planById.TryGetValue(s.PlanId, out var plan))
                        return 0m;

                    return plan.BillingPeriod == EnumBillingPeriod.Monthly
                        ? plan.Price
                        : plan.Price / 12m;
                });

                return Result<SubscriptionRevenueDto>.Success(new SubscriptionRevenueDto
                {
                    ActiveSubscriptionCount = subscriptions.Count,
                    Mrr = mrr,
                    Currency = plans.FirstOrDefault()?.Currency ?? string.Empty,
                    Plans = planDistribution
                });
            }
            catch (Exception ex)
            {
                return Result<SubscriptionRevenueDto>.Failure(
                    Error.Internal("Abonelik geliri hesaplanırken bir hata oluştu.", ex.Message));
            }
        }
    }
}
