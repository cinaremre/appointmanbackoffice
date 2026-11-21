using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Reports.Queries.GetSubscriptionRevenue
{
    /// <summary>
    /// Abonelik gelir handler'i (dokuman Modul 18). Aktif abonelikler
    /// BusinessSubscription.IsActiveAt semantigiyle (Active durum + gecerlilik
    /// araligi) secilir; plan bilgileri ayri repository'den eslestirilir.
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

                // 2. Plan bilgileri kod eslestirmesi.
                var plans = await _planRepository.GetAllAsync(cancellationToken);
                var planById = plans.ToDictionary(p => p.Id);

                // 3. Plan bazinda dagilim.
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

                // 4. MRR: Monthly tam fiyat, Yearly fiyatin 1/12'si.
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
                    Error.Internal("Abonelik geliri hesaplanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
