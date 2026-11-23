using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Reengagement.Queries.GetReengagementCandidates
{
    /// <summary>
    /// İşletmenin geri çağırma adaylarını listeler (doküman Modül 13).
    /// Aday: IntervalDays - LeadDays eşiği geçmiş, o hizmette gelecek
    /// randevusu olmayan ve cooldown içinde bildirim almamış müşteri.
    /// </summary>
    public class GetReengagementCandidatesQuery : IRequest<Result<List<ReengagementCandidateDto>>>
    {
        public Guid BusinessId { get; set; }
    }
}
