using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Employees.Commands.RejectMyTimeOff
{
    /// <summary>
    /// İşletme sahibinin izin reddi. Domain kuralları tek kaynaktan
    /// yürütülür: işlem mevcut
    /// <see cref="Rezervise.Application.Features.Employees.Commands.RejectTimeOff.RejectTimeOffCommand"/>
    /// üzerinden IMediator ile gerçek handler'a delege edilir.
    /// </summary>
    public class RejectMyTimeOffCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        public Guid TimeOffId { get; set; }

        /// <summary>Opsiyonel işlem gerekçesi; denetim kaydına yazılır.</summary>
        public string? Reason { get; set; }
    }
}
