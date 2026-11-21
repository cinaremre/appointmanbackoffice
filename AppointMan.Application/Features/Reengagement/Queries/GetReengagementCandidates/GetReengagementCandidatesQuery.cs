using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Reengagement.Queries.GetReengagementCandidates
{
    /// <summary>
    /// Isletmenin geri cagirma adaylarini listeler (dokuman Modul 13).
    /// Aday: IntervalDays - LeadDays esigi gecmis, o hizmette gelecek
    /// randevusu olmayan ve cooldown icinde bildirim almamis musteri.
    /// </summary>
    public class GetReengagementCandidatesQuery : IRequest<Result<List<ReengagementCandidateDto>>>
    {
        public Guid BusinessId { get; set; }
    }
}
