using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Sales.Queries.GetDailySalesSummary
{
    /// <summary>
    /// Bir UTC gununun satis/kasa ozeti. Tum tutarlar net degerdir
    /// (iadeler tahsilattan dusulur).
    /// </summary>
    public class GetDailySalesSummaryQuery : IRequest<Result<DailySalesSummaryDto>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Ozet istenen gun (UTC).</summary>
        public DateTime Date { get; set; }
    }

    public class DailySalesSummaryDto
    {
        public DateTime Date { get; set; }
        public Guid BusinessId { get; set; }

        /// <summary>Tamamlanan satis sayisi.</summary>
        public int SalesCount { get; set; }

        /// <summary>Gunun satis toplami (GrandTotal).</summary>
        public decimal TotalSales { get; set; }

        /// <summary>Net nakit tahsilat (iadeler dusulmus).</summary>
        public decimal CashCollected { get; set; }

        /// <summary>Net kart tahsilat (iadeler dusulmus).</summary>
        public decimal CardCollected { get; set; }

        /// <summary>Gunun gider toplami.</summary>
        public decimal TotalExpenses { get; set; }

        /// <summary>Gunun satislarinda henuz odenmemis bakiye.</summary>
        public decimal OutstandingReceivables { get; set; }

        /// <summary>Isletmede acik kasa oturumu var mi?</summary>
        public bool IsCashOpen { get; set; }
    }
}
