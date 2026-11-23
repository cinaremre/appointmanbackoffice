using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Commands.RescheduleAppointment
{
    public class RescheduleAppointmentCommand : IRequest<Result<Guid>>
    {
        public Guid AppointmentId { get; set; }
        public Guid UserId { get; set; }
        public Guid? NewEmployeeId { get; set; }   // Opsiyonel: çalışan değişebilir
        public DateTime NewAppointmentDate { get; set; }
        public string? RescheduledBy { get; set; }
    }
}