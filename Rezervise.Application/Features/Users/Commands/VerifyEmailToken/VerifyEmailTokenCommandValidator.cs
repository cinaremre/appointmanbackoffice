using FluentValidation;

namespace Rezervise.Application.Features.Users.Commands.VerifyEmailToken
{
    /// <summary>
    /// Dogrulama token'inin bicimini denetler.
    /// </summary>
    public class VerifyEmailTokenCommandValidator : AbstractValidator<VerifyEmailTokenCommand>
    {
        public VerifyEmailTokenCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("Dogrulama token'i bos olamaz.")
                .MaximumLength(128)
                .WithMessage("Dogrulama token'i gecersiz.");
        }
    }
}
