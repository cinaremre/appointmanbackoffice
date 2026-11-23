using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.CloseCashSession
{
    /// <summary>
    /// Açık kasa oturumunu kapatır. Beklenen nakit; açılış + nakit tahsilat
    /// + manuel giriş - manuel çıkış - nakit iade formülüyle handler
    /// tarafından hesaplanır. Kart tahsilatlar beklenen nakide dahil DEGILDIR.
    /// </summary>
    public class CloseCashSessionCommand : IRequest<Result<CashSessionCloseResult>>
    {
        public Guid CashSessionId { get; set; }

        /// <summary>Fiziksel olarak sayılan nakit.</summary>
        public decimal CountedCash { get; set; }
    }

    /// <summary>Kapanış özeti; fark raposunu taşır.</summary>
    public class CashSessionCloseResult
    {
        public Guid CashSessionId { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal CountedCash { get; set; }

        /// <summary>CountedCash - ExpectedCash; negatif ise kasada eksik vardır.</summary>
        public decimal Difference { get; set; }
    }
}
