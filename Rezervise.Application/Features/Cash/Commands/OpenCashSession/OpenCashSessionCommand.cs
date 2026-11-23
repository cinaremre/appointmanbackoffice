using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.OpenCashSession
{
    /// <summary>
    /// Kasada yeni oturum açar. Aynı kasada açık oturum varken ikinci
    /// açış Conflict döner; aynı kısıt DB'deki unique filtreli index ile
    /// de korunur.
    /// </summary>
    public class OpenCashSessionCommand : IRequest<Result<Guid>>
    {
        public Guid CashRegisterId { get; set; }

        /// <summary>Kasada bulunan açılış nakdi.</summary>
        public decimal OpeningCash { get; set; }
    }
}
