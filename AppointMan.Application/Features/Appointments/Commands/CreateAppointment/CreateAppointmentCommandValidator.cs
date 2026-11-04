using FluentValidation;

namespace AppointMan.Application.Features.Appointments.Commands.CreateAppointment
{
    public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
    {
        public CreateAppointmentCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanici ID bos olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("Isletme ID bos olamaz.");

            RuleFor(x => x.ServiceId)
                .NotEmpty()
                .WithMessage("Hizmet ID bos olamaz.");

            RuleFor(x => x.EmployeeId)
                .NotEmpty()
                .WithMessage("Calisan ID bos olamaz.");

            RuleFor(x => x.AppointmentDate)
                .NotEmpty()
                .WithMessage("Randevu tarihi bos olamaz.")
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Randevu tarihi gecmis olamaz.");

            RuleFor(x => x.AppointmentDate)
                .Must(BeValidHour)
                .WithMessage("Randevu saati 06:00-23:00 arasinda olmalidir.");

            RuleFor(x => x.AppointmentDate)
                .Must(BeMultipleOfFiveMinutes)
                .WithMessage("Randevu saati 5 dakikanin kati olmalidir.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notlar 500 karakterden uzun olamaz.");
        }

        private bool BeValidHour(DateTime date)
        {
            return date.Hour >= 6 && date.Hour < 23;
        }

        private bool BeMultipleOfFiveMinutes(DateTime date)
        {
            return date.Minute % 5 == 0;
        }
    }
}