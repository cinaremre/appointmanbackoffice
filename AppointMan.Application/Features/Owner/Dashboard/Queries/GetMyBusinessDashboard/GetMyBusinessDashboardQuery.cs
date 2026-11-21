using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Reporting.Queries.GetBusinessDashboard;
using AppointMan.Application.Features.Sales.Queries.GetDailySalesSummary;
using MediatR;

namespace AppointMan.Application.Features.Owner.Dashboard.Queries.GetMyBusinessDashboard
{
    /// <summary>
    /// Isletme sahibinin yonetici paneli. Sahiplik handler'da dogrulanir;
    /// kartlar mevcut <see cref="GetBusinessDashboardQuery"/> ve
    /// <see cref="GetDailySalesSummaryQuery"/> uzerinden IMediator ile
    /// tek kaynaktan hesaplanir.
    /// </summary>
    public class GetMyBusinessDashboardQuery : IRequest<Result<DashboardWithSummaryDto>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Kartlarin hesaplanacagi gun (UTC); bos ise bugun.</summary>
        public DateTime? Date { get; set; }
    }

    /// <summary>Isletme dashboard kartlari + ayni gunun satis/kasa ozeti.</summary>
    public class DashboardWithSummaryDto
    {
        /// <summary>Randevu/doluluk kartlari (Modul 15.1).</summary>
        public BusinessDashboardDto Dashboard { get; init; } = new();

        /// <summary>Ayni gunun net satis/kasa ozeti (Modul 16).</summary>
        public DailySalesSummaryDto DailySalesSummary { get; init; } = new();
    }
}
