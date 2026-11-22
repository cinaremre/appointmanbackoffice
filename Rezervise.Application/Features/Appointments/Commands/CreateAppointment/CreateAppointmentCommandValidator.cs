using Rezervise.Domain.Interfaces;
using FluentValidation;

namespace Rezervise.Application.Features.Appointments.Commands.CreateAppointment
{
    public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
    {
        private readonly IDateTimeService _dateTimeService;

        public CreateAppointmentCommandValidator(IDateTimeService dateTimeService)
        {
            _dateTimeService = dateTimeService;

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
                .WithMessage("Randevu tarihi bos olamaz.");

            RuleFor(x => x.AppointmentDate)
                .Must(BeInFuture)
                .WithMessage("Randevu tarihi gecmis olamaz.");

            RuleFor(x => x.AppointmentDate)
                .Must(BeValidLocalHour)
                .WithMessage("Randevu saati 06:00-23:59 arasinda olmalidir.");

            RuleFor(x => x.AppointmentDate)
                .Must(BeMultipleOfFiveLocalMinutes)
                .WithMessage("Randevu saati 5 dakikanin kati olmalidir.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notlar 500 karakterden uzun olamaz.");
        }

        // Istek tarihi UTC anidir; mesai saatleri ise isletmenin yerel duvar saatidir.
        private bool BeInFuture(DateTime date) => date > _dateTimeService.UtcNow;

        private bool BeValidLocalHour(DateTime date)
        {
            var local = _dateTimeService.ToBusinessLocalTime(date);
            return local.Hour >= 6 && local.Hour <= 23;
        }

        private bool BeMultipleOfFiveLocalMinutes(DateTime date)
        {
            var local = _dateTimeService.ToBusinessLocalTime(date);
            return local.Minute % 5 == 0;
        }
    }
}
