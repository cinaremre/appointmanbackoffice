using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.ConsumePackageSession
{
    /// <summary>
    /// Tamamlanan randevunun paket rezervasyonunu tüketime çevirir
    /// (doküman Modül 5). Randevu Completed olmalı; aynı randevunun aktif
    /// Reservation hareketi Cancelled yapılır ve yerine Consumption eklenir.
    /// </summary>
    public class ConsumePackageSessionCommand : IRequest<Result<Guid>>
    {
        public Guid CustomerPackageId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid AppointmentId { get; set; }
    }
}
