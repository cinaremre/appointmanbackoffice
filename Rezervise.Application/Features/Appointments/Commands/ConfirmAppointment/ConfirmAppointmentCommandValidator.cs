using FluentValidation;

namespace Rezervise.Application.Features.Appointments.Commands.ConfirmAppointment
{
    public class ConfirmAppointmentCommandValidator : AbstractValidator<ConfirmAppointmentCommand>
    {
        public ConfirmAppointmentCommandValidator()
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
