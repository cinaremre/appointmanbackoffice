using FluentValidation;

namespace Rezervise.Application.Features.Users.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanıcı ID boş olamaz.");

            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("Mevcut şifre boş olamaz.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("Yeni şifre boş olamaz.")
                .MinimumLength(6)
                .WithMessage("Yeni şifre en az 6 karakter olmalıdır.")
                .MaximumLength(50)
                .WithMessage("Yeni şifre en fazla 50 karakter olmalıdır.")
                .NotEqual(x => x.CurrentPassword)
                .WithMessage("Yeni şifre mevcut şifre ile aynı olamaz.");
        }
    }
}