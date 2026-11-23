using FluentValidation;

namespace Rezervise.Application.Features.Users.Commands.VerifyEmailToken
{
    /// <summary>
    /// Doğrulama token'inin biçimini denetler.
    /// </summary>
    public class VerifyEmailTokenCommandValidator : AbstractValidator<VerifyEmailTokenCommand>
    {
        public VerifyEmailTokenCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("Doğrulama token'i boş olamaz.")
                .MaximumLength(128)
                .WithMessage("Doğrulama token'i geçersiz.");
        }
    }
}
