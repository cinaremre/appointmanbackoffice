using AppointMan.Domain.Interfaces;
using FluentValidation;

namespace AppointMan.Application.Features.Appointments.Commands.RescheduleAppointment
{
    public class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
    {
        private readonly IDateTimeService _dateTimeService;

        public RescheduleAppointmentCommandValidator(IDateTimeService dateTimeService)
        {
            _dateTimeService = dateTimeService;

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
                .Must(BeInFuture)
                .WithMessage("Randevu tarihi gecmis olamaz.");

            RuleFor(x => x.NewAppointmentDate)
                .Must(BeValidLocalHour)
                .WithMessage("Randevu saati 06:00-23:59 arasinda olmalidir.");

            RuleFor(x => x.NewAppointmentDate)
                .Must(BeMultipleOfFiveLocalMinutes)
                .WithMessage("Randevu saati 5 dakikanin kati olmalidir.");

            RuleFor(x => x.NewEmployeeId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("Yeni calisan ID gecersiz.");
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
