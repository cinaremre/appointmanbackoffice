using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Appointments.Commands.CancelMyAppointment
{
    /// <summary>
    /// Isletme sahibinin kendi isletmesindeki randevuyu iptal etmesi.
    /// Domain kurallari tek kaynaktan yurutulur: iptal mevcut
    /// <see cref="Rezervise.Application.Features.Appointments.Commands.CancelAppointment.CancelAppointmentCommand"/>
    /// uzerinden IMediator ile gercek handler'a delege edilir.
    /// </summary>
    public class CancelMyAppointmentCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        public Guid AppointmentId { get; set; }

        /// <summary>Sahibin iptal gerekcesi; zorunludur, denetim kaydina yazilir.</summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Randevuya islenecek musterinin gorecegi iptal gerekcesi.
        /// Bos ise owner gerekcesi kullanilir.
        /// </summary>
        public string? CancellationReason { get; set; }
    }
}
