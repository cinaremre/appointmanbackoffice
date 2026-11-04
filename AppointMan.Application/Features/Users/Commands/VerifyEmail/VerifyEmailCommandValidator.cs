using FluentValidation;

namespace AppointMan.Application.Features.Users.Commands.VerifyEmail
{
    public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
    {
        public VerifyEmailCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanici ID bos olamaz.");

            RuleFor(x => x.VerificationCode)
                .NotEmpty()
                .WithMessage("Dogrulama kodu bos olamaz.")
                .Length(6)
                .WithMessage("Dogrulama kodu 6 karakter olmalidir.");
        }
    }
}