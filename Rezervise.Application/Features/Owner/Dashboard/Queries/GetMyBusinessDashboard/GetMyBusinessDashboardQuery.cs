using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Reporting.Queries.GetBusinessDashboard;
using Rezervise.Application.Features.Sales.Queries.GetDailySalesSummary;
using MediatR;

namespace Rezervise.Application.Features.Owner.Dashboard.Queries.GetMyBusinessDashboard
{
    /// <summary>
    /// İşletme sahibinin yönetici paneli. Sahiplik handler'da doğrulanır;
    /// kartlar mevcut <see cref="GetBusinessDashboardQuery"/> ve
    /// <see cref="GetDailySalesSummaryQuery"/> üzerinden IMediator ile
    /// tek kaynaktan hesaplanır.
    /// </summary>
    public class GetMyBusinessDashboardQuery : IRequest<Result<DashboardWithSummaryDto>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Kartların hesaplanacağı gün (UTC); boş ise bugün.</summary>
        public DateTime? Date { get; set; }
    }

    /// <summary>İşletme dashboard kartları + aynı günün satış/kasa özeti.</summary>
    public class DashboardWithSummaryDto
    {
        /// <summary>Randevu/doluluk kartları (Modül 15.1).</summary>
        public BusinessDashboardDto Dashboard { get; init; } = new();

        /// <summary>Aynı günün net satış/kasa özeti (Modül 16).</summary>
        public DailySalesSummaryDto DailySalesSummary { get; init; } = new();
    }
}
