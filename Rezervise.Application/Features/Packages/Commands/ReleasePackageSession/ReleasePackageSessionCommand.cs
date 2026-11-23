using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.ReleasePackageSession
{
    /// <summary>
    /// İptal edilen randevunun paket rezervasyonunu serbest bırakır
    /// (doküman Modül 5). Randevunun aktif Reservation hareketi
    /// Release'e çevrilir ve hak yeniden kullanılabilir olur.
    /// </summary>
    public class ReleasePackageSessionCommand : IRequest<Result<Guid>>
    {
        public Guid AppointmentId { get; set; }
    }
}
