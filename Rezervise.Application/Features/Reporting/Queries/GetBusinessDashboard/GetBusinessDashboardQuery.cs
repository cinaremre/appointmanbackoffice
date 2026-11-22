using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Reporting.Queries.GetBusinessDashboard
{
    /// <summary>
    /// Isletmenin bir gunluk yonetici dashboard kartlari (dokuman Modul 15.1).
    /// Date UTC gun olarak verilir; kartlar o gunun randevularindan hesaplanir.
    /// </summary>
    public class GetBusinessDashboardQuery : IRequest<Result<BusinessDashboardDto>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Raporlanacak gun (UTC; saat bilgisi yoksayilir).</summary>
        public DateTime Date { get; set; }
    }
}
