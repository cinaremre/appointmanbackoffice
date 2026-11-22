using FluentValidation;

namespace Rezervise.Application.Features.Users.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanici ID bos olamaz.");

            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("Mevcut sifre bos olamaz.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("Yeni sifre bos olamaz.")
                .MinimumLength(6)
                .WithMessage("Yeni sifre en az 6 karakter olmalidir.")
                .MaximumLength(50)
                .WithMessage("Yeni sifre en fazla 50 karakter olmalidir.")
                .NotEqual(x => x.CurrentPassword)
                .WithMessage("Yeni sifre mevcut sifre ile ayni olamaz.");
        }
    }
}