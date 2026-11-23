using FluentValidation;

namespace Rezervise.Application.Features.Users.Commands.ResendVerificationEmail
{
    /// <summary>
    /// Yeniden gönderim isteğindeki e-posta adresini denetler.
    /// </summary>
    public class ResendVerificationEmailCommandValidator : AbstractValidator<ResendVerificationEmailCommand>
    {
        public ResendVerificationEmailCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("E-posta adresi boş olamaz.")
                .EmailAddress()
                .WithMessage("Geçerli bir e-posta adresi giriniz.")
                .MaximumLength(255)
                .WithMessage("E-posta adresi en fazla 255 karakter olabilir.");
        }
    }
}
