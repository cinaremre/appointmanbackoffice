using Rezervise.Domain.Interfaces;
using FluentValidation;

namespace Rezervise.Application.Features.Appointments.Queries.GetAvailableSlots
{
    public class GetAvailableSlotsQueryValidator : AbstractValidator<GetAvailableSlotsQuery>
    {
        private readonly IDateTimeService _dateTimeService;

        public GetAvailableSlotsQueryValidator(IDateTimeService dateTimeService)
        {
            _dateTimeService = dateTimeService;

            RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme ID boş olamaz.");
            RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("Çalışan ID boş olamaz.");
            RuleFor(x => x.ServiceId).NotEmpty().WithMessage("Hizmet ID boş olamaz.");

            // Tarih UTC instant/date olarak gelir; gün karşılaştırması işletmenin
            // yerel gününde ve _dateTimeService'in referans saatiyle yapılır.
            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Tarih boş olamaz.")
                .Must(d => ToBusinessLocalDate(d) >= BusinessLocalToday)
                .WithMessage("Geçmiş tarih olamaz.")
                .Must(d => ToBusinessLocalDate(d) <= BusinessLocalToday.AddDays(90))
                .WithMessage("En fazla 90 gün ileriye randevu alınabilir.");
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
