using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.SuspendBusinessByAdmin
{
    /// <summary>
    /// Platform yoneticisinin isletmeyi askiya almasi (dokuman Modul 8).
    /// Gerekce zorunludur ve denetim kaydina yazilir.
    /// </summary>
    public class SuspendBusinessByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Askıya alma gerekcesi; bos gonderilemez.</summary>
        public string? Reason { get; set; }
    }
}
