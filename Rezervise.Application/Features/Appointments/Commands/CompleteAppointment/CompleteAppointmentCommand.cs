using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Commands.CompleteAppointment
{
    public class CompleteAppointmentCommand : IRequest<Result<Guid>>
    {
        public Guid AppointmentId { get; set; }
        public Guid BusinessId { get; set; }
        public string? CompletedBy { get; set; }
    }
}