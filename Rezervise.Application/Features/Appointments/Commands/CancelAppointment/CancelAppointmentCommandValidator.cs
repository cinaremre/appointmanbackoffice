using FluentValidation;

namespace Rezervise.Application.Features.Appointments.Commands.CancelAppointment
{
    public class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
    {
        public CancelAppointmentCommandValidator()
        {
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Randevu ID boş olamaz.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanıcı ID boş olamaz.");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("İptal gerekçesi 500 karakterden uzun olamaz.");
        }
    }
}
