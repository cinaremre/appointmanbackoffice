using AppointMan.Domain.Interfaces;
using FluentValidation;

namespace AppointMan.Application.Features.Appointments.Queries.GetAvailableSlots
{
    public class GetAvailableSlotsQueryValidator : AbstractValidator<GetAvailableSlotsQuery>
    {
        private readonly IDateTimeService _dateTimeService;

        public GetAvailableSlotsQueryValidator(IDateTimeService dateTimeService)
        {
            _dateTimeService = dateTimeService;

            RuleFor(x => x.BusinessId).NotEmpty().WithMessage("Isletme ID bos olamaz.");
            RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("Calisan ID bos olamaz.");
            RuleFor(x => x.ServiceId).NotEmpty().WithMessage("Hizmet ID bos olamaz.");

            // Tarih UTC instant/date olarak gelir; gun karsilastirmasi isletmenin
            // yerel gununde ve _dateTimeService'in referans saatiyle yapilir.
            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Tarih bos olamaz.")
                .Must(d => ToBusinessLocalDate(d) >= BusinessLocalToday)
                .WithMessage("Gecmis tarih olamaz.")
                .Must(d => ToBusinessLocalDate(d) <= BusinessLocalToday.AddDays(90))
                .WithMessage("En fazla 90 gun ileriye randevu alinabilir.");
        }

        private DateTime ToBusinessLocalDate(DateTime utcDateTime)
        {
            return _dateTimeService.ToBusinessLocalTime(utcDateTime).Date;
        }

        private DateTime BusinessLocalToday
        {
            get { return _dateTimeService.ToBusinessLocalTime(_dateTimeService.UtcNow).Date; }
        }
    }
}
