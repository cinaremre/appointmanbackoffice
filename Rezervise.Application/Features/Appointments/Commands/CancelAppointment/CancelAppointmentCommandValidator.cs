using FluentValidation;

namespace Rezervise.Application.Features.Appointments.Commands.CancelAppointment
{
    public class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
    {
        public CancelAppointmentCommandValidator()
        {
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Randevu ID bos olamaz.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanici ID bos olamaz.");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Iptal gerekcesi 500 karakterden uzun olamaz.");
        }
    }
}
