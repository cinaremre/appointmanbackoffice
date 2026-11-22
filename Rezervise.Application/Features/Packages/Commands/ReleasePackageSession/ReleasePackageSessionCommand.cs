using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.ReleasePackageSession
{
    /// <summary>
    /// Iptal edilen randevunun paket rezervasyonunu serbest birakir
    /// (dokuman Modul 5). Randevunun aktif Reservation hareketi
    /// Release'e cevrilir ve hak yeniden kullanilabilir olur.
    /// </summary>
    public class ReleasePackageSessionCommand : IRequest<Result<Guid>>
    {
        public Guid AppointmentId { get; set; }
    }
}
