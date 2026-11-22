using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Commands.MarkAppointmentNoShow
{
    /// <summary>
    /// Randevuyu "gelmedi" (no-show) olarak isaretler.
    /// </summary>
    public class MarkAppointmentNoShowCommand : IRequest<Result<Guid>>
    {
        /// <summary>Isaretlenecek randevu kimligi.</summary>
        public Guid AppointmentId { get; set; }

        /// <summary>Istegi yapan kullanici (JWT'den yazilir).</summary>
        public Guid UserId { get; set; }

        /// <summary>Islemi yapan kullanici etiketi (gecmis kaydinda gorunur).</summary>
        public string? ChangedBy { get; set; }
    }
}
