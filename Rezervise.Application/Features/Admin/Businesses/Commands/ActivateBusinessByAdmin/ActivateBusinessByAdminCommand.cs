using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.ActivateBusinessByAdmin
{
    /// <summary>
    /// Platform yöneticisinin askıdaki işletmeyi yeniden aktif etmesi
    /// (doküman Modül 8). Denetim kaydına "Activate" aksiyonuyla yazılır.
    /// </summary>
    public class ActivateBusinessByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Aktif etme gerekçesi; opsiyonel, denetim kaydına yazılır.</summary>
        public string? Reason { get; set; }
    }
}
