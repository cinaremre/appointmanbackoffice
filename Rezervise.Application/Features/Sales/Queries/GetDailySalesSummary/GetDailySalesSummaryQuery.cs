using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Sales.Queries.GetDailySalesSummary
{
    /// <summary>
    /// Bir UTC gününün satış/kasa özeti. Tüm tutarlar net değerdir
    /// (iadeler tahsilattan düşülür).
    /// </summary>
    public class GetDailySalesSummaryQuery : IRequest<Result<DailySalesSummaryDto>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Özet istenen gün (UTC).</summary>
        public DateTime Date { get; set; }
    }

    public class DailySalesSummaryDto
    {
        public DateTime Date { get; set; }
        public Guid BusinessId { get; set; }

        /// <summary>Tamamlanan satış sayısı.</summary>
        public int SalesCount { get; set; }

        /// <summary>Günün satış toplamı (GrandTotal).</summary>
        public decimal TotalSales { get; set; }

        /// <summary>Net nakit tahsilat (iadeler düşülmüş).</summary>
        public decimal CashCollected { get; set; }

        /// <summary>Net kart tahsilat (iadeler düşülmüş).</summary>
        public decimal CardCollected { get; set; }

        /// <summary>Günün gider toplamı.</summary>
        public decimal TotalExpenses { get; set; }

        /// <summary>Günün satışlarında henüz ödenmemiş bakiye.</summary>
        public decimal OutstandingReceivables { get; set; }

        /// <summary>İşletmede açık kasa oturumu var mi?</summary>
        public bool IsCashOpen { get; set; }
    }
}
