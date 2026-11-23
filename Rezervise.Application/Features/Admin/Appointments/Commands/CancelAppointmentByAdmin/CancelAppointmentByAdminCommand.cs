using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Appointments.Commands.CancelAppointmentByAdmin
{
    /// <summary>
    /// Yönetici iptali (doküman Modül 12.2). Domain kontrolü tek kaynaktan
    /// yürütülür: iptal, mevcut CancelAppointmentCommand üzerinden
    /// gerçek CancelAppointmentCommandHandler'a delege edilir.
    ///
    /// DOKUMAN 12.3: appointment.override (geçmiş/randevu kurallarını aşarak
    /// iptal) ilk sürümde kapsam dışıdır; ayrı bir komutla eklenecektir.
    /// </summary>
    public class CancelAppointmentByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid AppointmentId { get; set; }

        /// <summary>Yöneticinin iptal gerekçesi; zorunludur, denetim kaydına yazılır.</summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Randevuya işlenecek müşterinin göreceği iptal gerekçesi.
        /// Boş ise admin gerekçesi kullanılır.
        /// </summary>
        public string? CancellationReason { get; set; }
    }
}
