using Rezervise.Domain.Interfaces;
using FluentValidation;

namespace Rezervise.Application.Features.Appointments.Commands.RescheduleAppointment
{
    public class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
    {
        private readonly IDateTimeService _dateTimeService;

        public RescheduleAppointmentCommandValidator(IDateTimeService dateTimeService)
        {
            _dateTimeService = dateTimeService;

            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Randevu ID boş olamaz.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanıcı ID boş olamaz.");

            RuleFor(x => x.NewAppointmentDate)
                .NotEmpty()
                .WithMessage("Yeni randevu tarihi boş olamaz.");

            RuleFor(x => x.NewAppointmentDate)
                .Must(BeInFuture)
                .WithMessage("Randevu tarihi geçmiş olamaz.");

            RuleFor(x => x.NewAppointmentDate)
                .Must(BeValidLocalHour)
                .WithMessage("Randevu saati 06:00-23:59 arasında olmalıdır.");

            RuleFor(x => x.NewAppointmentDate)
                .Must(BeMultipleOfFiveLocalMinutes)
                .WithMessage("Randevu saati 5 dakikanın katı olmalıdır.");

            RuleFor(x => x.NewEmployeeId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("Yeni çalışan ID geçersiz.");
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
