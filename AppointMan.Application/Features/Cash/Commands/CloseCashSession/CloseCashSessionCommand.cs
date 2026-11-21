using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Cash.Commands.CloseCashSession
{
    /// <summary>
    /// Acik kasa oturumunu kapatir. Beklenen nakit; acilis + nakit tahsilat
    /// + manuel giris - manuel cikis - nakit iade formuluyle handler
    /// tarafindan hesaplanir. Kart tahsilatlar beklenen nakide dahil DEGILDIR.
    /// </summary>
    public class CloseCashSessionCommand : IRequest<Result<CashSessionCloseResult>>
    {
        public Guid CashSessionId { get; set; }

        /// <summary>Fiziksel olarak sayilan nakit.</summary>
        public decimal CountedCash { get; set; }
    }

    /// <summary>Kapanis ozeti; fark raposunu tasir.</summary>
    public class CashSessionCloseResult
    {
        public Guid CashSessionId { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal CountedCash { get; set; }

        /// <summary>CountedCash - ExpectedCash; negatif ise kasada eksik vardir.</summary>
        public decimal Difference { get; set; }
    }
}
