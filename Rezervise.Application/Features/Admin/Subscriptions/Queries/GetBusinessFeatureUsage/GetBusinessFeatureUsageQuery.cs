using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Queries.GetBusinessFeatureUsage
{
    /// <summary>
    /// İşletmenin özellik bazında kullanım sayıları (doküman Modül 13).
    /// Kapsam: su anki ay dahil son uç dönem (YYYY-MM biçiminde);
    /// FeatureUsage sayaçları FeatureDefinition ile eşlenerek dondurulur.
    /// </summary>
    public class GetBusinessFeatureUsageQuery : IRequest<Result<IReadOnlyList<AdminFeatureUsageDto>>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>Tek dönem, tek özellik kullanım satırı.</summary>
    public class AdminFeatureUsageDto
    {
        public Guid FeatureId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;

        /// <summary>Kullanım dönemi, "YYYY-MM" biçiminde.</summary>
        public string UsagePeriod { get; set; } = string.Empty;

        public int UsedQuantity { get; set; }
    }
}
