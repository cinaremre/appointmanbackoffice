using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Commands.ConfirmAppointment
{
    public class ConfirmAppointmentCommand : IRequest<Result<Guid>>
    {
        public Guid AppointmentId { get; set; }
        public Guid BusinessId { get; set; }
        public string? ConfirmedBy { get; set; }
    }
}