using FluentValidation;

namespace AppointMan.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("Isim bos olamaz.")
                .MinimumLength(2)
                .WithMessage("Isim en az 2 karakter olmalidir.")
                .MaximumLength(50)
                .WithMessage("Isim en fazla 50 karakter olmalidir.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Soyisim bos olamaz.")
                .MinimumLength(2)
                .WithMessage("Soyisim en az 2 karakter olmalidir.")
                .MaximumLength(50)
                .WithMessage("Soyisim en fazla 50 karakter olmalidir.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email bos olamaz.")
                .EmailAddress()
                .WithMessage("Gecersiz email format.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Telefon numarasi bos olamaz.")
                .MinimumLength(10)
                .WithMessage("Telefon numarasi en az 10 karakter olmalidir.")
                .MaximumLength(15)
                .WithMessage("Telefon numarasi en fazla 15 karakter olmalidir.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Sifre bos olamaz.")
                .MinimumLength(6)
                .WithMessage("Sifre en az 6 karakter olmalidir.")
                .MaximumLength(50)
                .WithMessage("Sifre en fazla 50 karakter olmalidir.");

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Gecersiz kullanici rol.");

            RuleFor(x => x.DateOfBirth)
                .Must(date => !date.HasValue || date.Value < DateTime.UtcNow.AddYears(-18))
                .WithMessage("Yas 18'den buyuk olmalidir.");
        }
    }
}