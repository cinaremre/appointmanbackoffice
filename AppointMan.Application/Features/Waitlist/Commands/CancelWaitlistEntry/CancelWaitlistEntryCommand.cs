using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Waitlist.Commands.CancelWaitlistEntry
{
    /// <summary>
    /// Bekleme listesi kaydini iptal eder (dokuman Modul 10).
    /// Yetki: musterinin kendisi veya isletme sahibi.
    /// </summary>
    public class CancelWaitlistEntryCommand : IRequest<Result<Guid>>
    {
        public Guid EntryId { get; set; }
    }
}
