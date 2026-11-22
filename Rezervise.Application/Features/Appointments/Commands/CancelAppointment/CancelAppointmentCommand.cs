using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Commands.CancelAppointment
{
    public class CancelAppointmentCommand : IRequest<Result<Guid>>
    {
        public Guid AppointmentId { get; set; }
        public Guid UserId { get; set; }
        public string? Reason { get; set; }
        public string? CancelledBy { get; set; }
    }
}