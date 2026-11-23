using FluentValidation;

namespace Rezervise.Application.Features.WorkingHours.Commands.AddWorkingHour
{
    public class AddWorkingHourCommandValidator : AbstractValidator<AddWorkingHourCommand>
    {
        public AddWorkingHourCommandValidator()
        {
            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("İşletme ID boş olamaz.");

            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Sahip ID boş olamaz.");

            RuleFor(x => x.DayOfWeek)
                .IsInEnum()
                .WithMessage("Geçersiz gün.");

            RuleFor(x => x.StartTime)
                .Must(t => t >= TimeSpan.Zero && t < TimeSpan.FromHours(24))
                .WithMessage("Başlangıç saati 00:00-24:00 arasında olmalıdır.");

            RuleFor(x => x.EndTime)
                .Must(t => t >= TimeSpan.Zero && t < TimeSpan.FromHours(24))
                .WithMessage("Bitiş saati 00:00-24:00 arasında olmalıdır.");

            RuleFor(x => x)
                .Must(x => x.StartTime < x.EndTime)
                .WithMessage("Başlangıç saati bitiş saatinden önce olmalıdır.");

            RuleFor(x => x.StartTime)
                .Must(t => t.Minutes % 5 == 0)
                .WithMessage("Başlangıç saati 5 dakikanın katı olmalıdır.");

            RuleFor(x => x.EndTime)
                .Must(t => t.Minutes % 5 == 0)
                .WithMessage("Bitiş saati 5 dakikanın katı olmalıdır.");
        }
    }
}