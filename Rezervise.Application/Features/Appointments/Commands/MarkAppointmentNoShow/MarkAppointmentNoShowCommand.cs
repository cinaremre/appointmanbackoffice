using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Commands.MarkAppointmentNoShow
{
    /// <summary>
    /// Randevuyu "gelmedi" (no-show) olarak işaretler.
    /// </summary>
    public class MarkAppointmentNoShowCommand : IRequest<Result<Guid>>
    {
        /// <summary>İşaretlenecek randevu kimliği.</summary>
        public Guid AppointmentId { get; set; }

        /// <summary>İsteği yapan kullanıcı (JWT'den yazılır).</summary>
        public Guid UserId { get; set; }

        /// <summary>İşlemi yapan kullanıcı etiketi (geçmiş kaydında görünür).</summary>
        public string? ChangedBy { get; set; }
    }
}
