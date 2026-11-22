using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.OpenCashSession
{
    /// <summary>
    /// Kasada yeni oturum acar. Ayni kasada acik oturum varken ikinci
    /// acis Conflict doner; ayni kisit DB'deki unique filtreli index ile
    /// de korunur.
    /// </summary>
    public class OpenCashSessionCommand : IRequest<Result<Guid>>
    {
        public Guid CashRegisterId { get; set; }

        /// <summary>Kasada bulunan acilis nakdi.</summary>
        public decimal OpeningCash { get; set; }
    }
}
