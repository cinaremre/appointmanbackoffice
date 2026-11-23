using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Appointments.Commands.CancelMyAppointment
{
    /// <summary>
    /// İşletme sahibinin kendi işletmesindeki randevuyu iptal etmesi.
    /// Domain kuralları tek kaynaktan yürütülür: iptal mevcut
    /// <see cref="Rezervise.Application.Features.Appointments.Commands.CancelAppointment.CancelAppointmentCommand"/>
    /// üzerinden IMediator ile gerçek handler'a delege edilir.
    /// </summary>
    public class CancelMyAppointmentCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        public Guid AppointmentId { get; set; }

        /// <summary>Sahibin iptal gerekçesi; zorunludur, denetim kaydına yazılır.</summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Randevuya işlenecek müşterinin göreceği iptal gerekçesi.
        /// Boş ise owner gerekçesi kullanılır.
        /// </summary>
        public string? CancellationReason { get; set; }
    }
}
