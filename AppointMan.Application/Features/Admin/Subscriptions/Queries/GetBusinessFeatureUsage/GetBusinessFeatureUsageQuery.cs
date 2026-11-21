using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Subscriptions.Queries.GetBusinessFeatureUsage
{
    /// <summary>
    /// Isletmenin ozellik bazinda kullanim sayilari (dokuman Modul 13).
    /// Kapsam: su anki ay dahil son uc donem (YYYY-MM biciminde);
    /// FeatureUsage sayaclari FeatureDefinition ile eslenerek dondurulur.
    /// </summary>
    public class GetBusinessFeatureUsageQuery : IRequest<Result<IReadOnlyList<AdminFeatureUsageDto>>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>Tek donem, tek ozellik kullanim satiri.</summary>
    public class AdminFeatureUsageDto
    {
        public Guid FeatureId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;

        /// <summary>Kullanim donemi, "YYYY-MM" biciminde.</summary>
        public string UsagePeriod { get; set; } = string.Empty;

        public int UsedQuantity { get; set; }
    }
}
