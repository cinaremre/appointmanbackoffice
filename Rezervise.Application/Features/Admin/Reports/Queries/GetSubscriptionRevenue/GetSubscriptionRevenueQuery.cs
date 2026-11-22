using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Reports.Queries.GetSubscriptionRevenue
{
    /// <summary>
    /// Abonelik gelir raporu (dokuman Modul 18). Aktif abonelik sayisi,
    /// plan bazinda dagilim ve normalize aylik yinelenen gelir (MRR).
    /// </summary>
    public class GetSubscriptionRevenueQuery : IRequest<Result<SubscriptionRevenueDto>>
    {
    }

    /// <summary>Abonelik gelir ozeti.</summary>
    public class SubscriptionRevenueDto
    {
        /// <summary>Su anda aktif abonelik sayisi.</summary>
        public int ActiveSubscriptionCount { get; init; }

        /// <summary>
        /// Aylik yinelenen gelir: Monthly planlar tam fiyat,
        /// Yearly planlar fiyat/12 olarak toplanir.
        /// </summary>
        public decimal Mrr { get; init; }

        /// <summary>Ilk planin para birim kodu; plan yoksa bos metin.</summary>
        public string Currency { get; init; } = string.Empty;

        /// <summary>Plan bazinda aktif isletme dagilimi.</summary>
        public IReadOnlyList<PlanDistributionDto> Plans { get; init; } = Array.Empty<PlanDistributionDto>();
    }

    /// <summary>Tek bir planin dagilim satiri.</summary>
    public class PlanDistributionDto
    {
        public Guid PlanId { get; init; }

        public string PlanName { get; init; } = string.Empty;

        /// <summary>Bu plana aktif abone isletme sayisi.</summary>
        public int BusinessCount { get; init; }
    }
}
