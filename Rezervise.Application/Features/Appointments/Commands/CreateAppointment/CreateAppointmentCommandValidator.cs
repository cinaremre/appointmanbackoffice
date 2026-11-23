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
                .WithMessage("Kullanıcı ID boş olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("İşletme ID boş olamaz.");

            RuleFor(x => x.ServiceId)
                .NotEmpty()
                .WithMessage("Hizmet ID boş olamaz.");

            RuleFor(x => x.EmployeeId)
                .NotEmpty()
                .WithMessage("Çalışan ID boş olamaz.");

            RuleFor(x => x.AppointmentDate)
                .NotEmpty()
                .WithMessage("Randevu tarihi boş olamaz.");

            RuleFor(x => x.AppointmentDate)
                .Must(BeInFuture)
                .WithMessage("Randevu tarihi geçmiş olamaz.");

            RuleFor(x => x.AppointmentDate)
                .Must(BeValidLocalHour)
                .WithMessage("Randevu saati 06:00-23:59 arasında olmalıdır.");

            RuleFor(x => x.AppointmentDate)
                .Must(BeMultipleOfFiveLocalMinutes)
                .WithMessage("Randevu saati 5 dakikanın katı olmalıdır.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notlar 500 karakterden uzun olamaz.");
        }

        // İstek tarihi UTC anıdır; mesai saatleri ise işletmenin yerel duvar saatidir.
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
