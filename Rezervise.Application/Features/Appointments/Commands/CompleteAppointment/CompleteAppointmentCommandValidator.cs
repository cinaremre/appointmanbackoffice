using FluentValidation;

namespace Rezervise.Application.Features.Appointments.Commands.CompleteAppointment
{
    public class CompleteAppointmentCommandValidator : AbstractValidator<CompleteAppointmentCommand>
    {
        public CompleteAppointmentCommandValidator()
        {
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Randevu ID boş olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("İşletme ID boş olamaz.");
        }
    }
}
