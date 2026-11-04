using FluentValidation;

namespace AppointMan.Application.Features.WorkingHours.Commands.AddWorkingHour
{
    public class AddWorkingHourCommandValidator : AbstractValidator<AddWorkingHourCommand>
    {
        public AddWorkingHourCommandValidator()
        {
            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("Isletme ID bos olamaz.");

            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Sahip ID bos olamaz.");

            RuleFor(x => x.DayOfWeek)
                .IsInEnum()
                .WithMessage("Gecersiz gun.");

            RuleFor(x => x.StartTime)
                .Must(t => t >= TimeSpan.Zero && t < TimeSpan.FromHours(24))
                .WithMessage("Baslangic saati 00:00-24:00 arasinda olmalidir.");

            RuleFor(x => x.EndTime)
                .Must(t => t >= TimeSpan.Zero && t < TimeSpan.FromHours(24))
                .WithMessage("Bitis saati 00:00-24:00 arasinda olmalidir.");

            RuleFor(x => x)
                .Must(x => x.StartTime < x.EndTime)
                .WithMessage("Baslangic saati bitis saatinden once olmalidir.");

            RuleFor(x => x.StartTime)
                .Must(t => t.Minutes % 5 == 0)
                .WithMessage("Baslangic saati 5 dakikanin kati olmalidir.");

            RuleFor(x => x.EndTime)
                .Must(t => t.Minutes % 5 == 0)
                .WithMessage("Bitis saati 5 dakikanin kati olmalidir.");
        }
    }
}