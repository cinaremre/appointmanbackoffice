using FluentValidation;

namespace AppointMan.Application.Features.Appointments.Queries.GetAvailableSlots
{
    public class GetAvailableSlotsQueryValidator : AbstractValidator<GetAvailableSlotsQuery>
    {
        public GetAvailableSlotsQueryValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty().WithMessage("Isletme ID bos olamaz.");
            RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("Calisan ID bos olamaz.");
            RuleFor(x => x.ServiceId).NotEmpty().WithMessage("Hizmet ID bos olamaz.");
            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Tarih bos olamaz.")
                .Must(d => d.Date >= DateTime.UtcNow.Date)
                .WithMessage("Gecmis tarih olamaz.")
                .Must(d => d.Date <= DateTime.UtcNow.Date.AddDays(90))
                .WithMessage("En fazla 90 gun ileriye randevu alinabilir.");
        }
    }
}