using FluentValidation;

namespace Rezervise.Application.Features.Users.Commands.ResendVerificationEmail
{
    /// <summary>
    /// Yeniden gonderim istegindeki e-posta adresini denetler.
    /// </summary>
    public class ResendVerificationEmailCommandValidator : AbstractValidator<ResendVerificationEmailCommand>
    {
        public ResendVerificationEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("E-posta adresi bos olamaz.")
                .EmailAddress()
                .WithMessage("Gecerli bir e-posta adresi giriniz.")
                .MaximumLength(255)
                .WithMessage("E-posta adresi en fazla 255 karakter olabilir.");
        }
    }
}
