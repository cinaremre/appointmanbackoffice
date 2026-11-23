using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.ReservePackageSession
{
    /// <summary>
    /// Randevu için paketten seans hakkı ayırır (doküman Modül 5).
    /// Aynı randevu için ikinci rezervasyon Conflict; hak asımında Conflict.
    /// </summary>
    public class ReservePackageSessionCommand : IRequest<Result<Guid>>
    {
        public Guid CustomerPackageId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid AppointmentId { get; set; }
    }
}
