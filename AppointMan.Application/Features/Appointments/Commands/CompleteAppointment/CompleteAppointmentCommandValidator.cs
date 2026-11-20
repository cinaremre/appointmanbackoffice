using FluentValidation;

namespace AppointMan.Application.Features.Appointments.Commands.CompleteAppointment
{
    public class CompleteAppointmentCommandValidator : AbstractValidator<CompleteAppointmentCommand>
    {
        public CompleteAppointmentCommandValidator()
        {
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Randevu ID bos olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("Isletme ID bos olamaz.");
        }
    }
}
