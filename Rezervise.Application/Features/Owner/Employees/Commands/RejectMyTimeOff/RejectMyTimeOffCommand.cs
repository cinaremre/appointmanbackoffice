using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Employees.Commands.RejectMyTimeOff
{
    /// <summary>
    /// Isletme sahibinin izin reddi. Domain kurallari tek kaynaktan
    /// yurutulur: islem mevcut
    /// <see cref="Rezervise.Application.Features.Employees.Commands.RejectTimeOff.RejectTimeOffCommand"/>
    /// uzerinden IMediator ile gercek handler'a delege edilir.
    /// </summary>
    public class RejectMyTimeOffCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        public Guid TimeOffId { get; set; }

        /// <summary>Opsiyonel islem gerekcesi; denetim kaydina yazilir.</summary>
        public string? Reason { get; set; }
    }
}
