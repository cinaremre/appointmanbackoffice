using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Packages.Commands.ConsumePackageSession
{
    /// <summary>
    /// Tamamlanan randevunun paket rezervasyonunu tuketime cevirir
    /// (dokuman Modul 5). Randevu Completed olmali; ayni randevunun aktif
    /// Reservation hareketi Cancelled yapilir ve yerine Consumption eklenir.
    /// </summary>
    public class ConsumePackageSessionCommand : IRequest<Result<Guid>>
    {
        public Guid CustomerPackageId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid AppointmentId { get; set; }
    }
}
