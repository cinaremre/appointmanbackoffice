using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.SuspendBusinessByAdmin
{
    /// <summary>
    /// Platform yöneticisinin işletmeyi askıya alması (doküman Modül 8).
    /// Gerekçe zorunludur ve denetim kaydına yazılır.
    /// </summary>
    public class SuspendBusinessByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Askıya alma gerekçesi; boş gönderilemez.</summary>
        public string? Reason { get; set; }
    }
}
