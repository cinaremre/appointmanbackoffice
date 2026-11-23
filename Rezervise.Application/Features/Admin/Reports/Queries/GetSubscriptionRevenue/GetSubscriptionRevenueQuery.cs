using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Reports.Queries.GetSubscriptionRevenue
{
    /// <summary>
    /// Abonelik gelir raporu (doküman Modül 18). Aktif abonelik sayısı,
    /// plan bazında dağılım ve normalize aylık yinelenen gelir (MRR).
    /// </summary>
    public class GetSubscriptionRevenueQuery : IRequest<Result<SubscriptionRevenueDto>>
    {
    }

    /// <summary>Abonelik gelir özeti.</summary>
    public class SubscriptionRevenueDto
    {
        /// <summary>Su anda aktif abonelik sayısı.</summary>
        public int ActiveSubscriptionCount { get; init; }

        /// <summary>
        /// Aylık yinelenen gelir: Monthly planlar tam fiyat,
        /// Yearly planlar fiyat/12 olarak toplanır.
        /// </summary>
        public decimal Mrr { get; init; }

        /// <summary>İlk planın para birim kodu; plan yoksa boş metin.</summary>
        public string Currency { get; init; } = string.Empty;

        /// <summary>Plan bazında aktif işletme dağılımı.</summary>
        public IReadOnlyList<PlanDistributionDto> Plans { get; init; } = Array.Empty<PlanDistributionDto>();
    }

    /// <summary>Tek bir planın dağılım satırı.</summary>
    public class PlanDistributionDto
    {
        public Guid PlanId { get; init; }

        public string PlanName { get; init; } = string.Empty;

        /// <summary>Bu plana aktif abone işletme sayısı.</summary>
        public int BusinessCount { get; init; }
    }
}
