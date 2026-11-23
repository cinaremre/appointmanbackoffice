using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Reporting.Queries.GetBusinessDashboard
{
    /// <summary>
    /// İşletmenin bir günlük yönetici dashboard kartları (doküman Modül 15.1).
    /// Date UTC gün olarak verilir; kartlar o günün randevularından hesaplanır.
    /// </summary>
    public class GetBusinessDashboardQuery : IRequest<Result<BusinessDashboardDto>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Raporlanacak gün (UTC; saat bilgisi yoksayılır).</summary>
        public DateTime Date { get; set; }
    }
}
