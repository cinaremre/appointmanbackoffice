using FluentValidation;

namespace AppointMan.Application.Features.Appointments.Commands.RescheduleAppointment
{
    public class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
    {
        public RescheduleAppointmentCommandValidator()
        {
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Randevu ID bos olamaz.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanici ID bos olamaz.");

            RuleFor(x => x.NewAppointmentDate)
                .NotEmpty()
                .WithMessage("Yeni randevu tarihi bos olamaz.");

            RuleFor(x => x.NewAppointmentDate)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Randevu tarihi gecmis olamaz.");

            RuleFor(x => x.NewAppointmentDate)
                .Must(BeValidHour)
                .WithMessage("Randevu saati 06:00-23:00 arasinda olmalidir.");

            RuleFor(x => x.NewAppointmentDate)
                .Must(BeMultipleOfFiveMinutes)
                .WithMessage("Randevu saati 5 dakikanin kati olmalidir.");

            RuleFor(x => x.NewEmployeeId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("Yeni calisan ID gecersiz.");
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