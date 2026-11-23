using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Waitlist.Commands.CancelWaitlistEntry
{
    /// <summary>
    /// Bekleme listesi kaydını iptal eder (doküman Modül 10).
    /// Yetki: müşterinin kendisi veya işletme sahibi.
    /// </summary>
    public class CancelWaitlistEntryCommand : IRequest<Result<Guid>>
    {
        public Guid EntryId { get; set; }
    }
}
