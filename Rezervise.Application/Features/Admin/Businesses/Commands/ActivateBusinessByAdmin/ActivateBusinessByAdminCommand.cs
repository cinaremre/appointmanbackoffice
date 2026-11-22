using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.ActivateBusinessByAdmin
{
    /// <summary>
    /// Platform yoneticisinin askidaki isletmeyi yeniden aktif etmesi
    /// (dokuman Modul 8). Denetim kaydina "Activate" aksiyonuyla yazilir.
    /// </summary>
    public class ActivateBusinessByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Aktif etme gerekcesi; opsiyonel, denetim kaydina yazilir.</summary>
        public string? Reason { get; set; }
    }
}
