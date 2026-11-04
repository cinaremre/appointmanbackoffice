using FluentValidation;

namespace AppointMan.Application.Features.Businesses.Commands.UpdateBusiness
{
    public class UpdateBusinessCommandValidator : AbstractValidator<UpdateBusinessCommand>
    {
        public UpdateBusinessCommandValidator()
        {
            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("Isletme ID bos olamaz.");

            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Sahip ID bos olamaz.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Isletme adi bos olamaz.")
                .MinimumLength(2)
                .WithMessage("Isletme adi en az 2 karakter olmalidir.")
                .MaximumLength(100)
                .WithMessage("Isletme adi en fazla 100 karakter olmalidir.");

            RuleFor(x => x.BusinessType)
                .IsInEnum()
                .WithMessage("Gecersiz isletme tipi.");

            RuleFor(x => x.Address)
                .NotNull()
                .WithMessage("Adres bos olamaz.");

            RuleFor(x => x.PhoneNumber)
                .NotNull()
                .WithMessage("Telefon numarasi bos olamaz.");

            RuleFor(x => x.Email)
                .NotNull()
                .WithMessage("Email bos olamaz.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Aciklama en fazla 500 karakter olmalidir.");

            RuleFor(x => x.WebsiteUrl)
                .MaximumLength(200)
                .WithMessage("Website URL en fazla 200 karakter olmalidir.");
        }
    }
}