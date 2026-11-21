using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Packages.Commands.ReservePackageSession
{
    /// <summary>
    /// Randevu icin paketten seans hakki ayirir (dokuman Modul 5).
    /// Ayni randevu icin ikinci rezervasyon Conflict; hak asiminda Conflict.
    /// </summary>
    public class ReservePackageSessionCommand : IRequest<Result<Guid>>
    {
        public Guid CustomerPackageId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid AppointmentId { get; set; }
    }
}
