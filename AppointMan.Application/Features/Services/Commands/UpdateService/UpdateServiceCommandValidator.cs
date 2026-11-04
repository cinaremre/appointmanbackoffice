using FluentValidation;

namespace AppointMan.Application.Features.Services.Commands.UpdateService
{
    public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
    {
        public UpdateServiceCommandValidator()
        {
            RuleFor(x => x.ServiceId)
                .NotEmpty()
                .WithMessage("Hizmet ID bos olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("Isletme ID bos olamaz.");

            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Sahip ID bos olamaz.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Hizmet adi bos olamaz.")
                .MinimumLength(2)
                .WithMessage("Hizmet adi en az 2 karakter olmalidir.")
                .MaximumLength(100)
                .WithMessage("Hizmet adi en fazla 100 karakter olmalidir.");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Fiyat 0'dan buyuk olmalidir.");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Para birimi bos olamaz.")
                .Length(3)
                .WithMessage("Para birimi 3 karakter olmalidir.");

            RuleFor(x => x.DurationInMinutes)
                .InclusiveBetween(5, 480)
                .WithMessage("Sure 5-480 dakika arasinda olmalidir.")
                .Must(d => d % 5 == 0)
                .WithMessage("Sure 5 dakikanin kati olmalidir.");

            RuleFor(x => x.Category)
                .IsInEnum()
                .WithMessage("Gecersiz hizmet kategorisi.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Aciklama en fazla 500 karakter olmalidir.");
        }
    }
}