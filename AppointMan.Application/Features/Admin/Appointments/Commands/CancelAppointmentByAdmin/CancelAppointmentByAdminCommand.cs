using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Appointments.Commands.CancelAppointmentByAdmin
{
    /// <summary>
    /// Yonetici iptali (dokuman Modul 12.2). Domain kontrolu tek kaynaktan
    /// yurutulur: iptal, mevcut CancelAppointmentCommand uzerinden
    /// gercek CancelAppointmentCommandHandler'a delege edilir.
    ///
    /// DOKUMAN 12.3: appointment.override (gecmis/randevu kurallarini asarak
    /// iptal) ilk surumde kapsam disidir; ayri bir komutla eklenecektir.
    /// </summary>
    public class CancelAppointmentByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid AppointmentId { get; set; }

        /// <summary>Yoneticinin iptal gerekcesi; zorunludur, denetim kaydina yazilir.</summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Randevuya islenecek musterinin gorecegi iptal gerekcesi.
        /// Bos ise admin gerekcesi kullanilir.
        /// </summary>
        public string? CancellationReason { get; set; }
    }
}
