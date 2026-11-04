using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Commands.CompleteAppointment
{
    public class CompleteAppointmentCommand : IRequest<Result<Guid>>
    {
        public Guid AppointmentId { get; set; }
        public Guid BusinessId { get; set; }
        public string? CompletedBy { get; set; }
    }
}