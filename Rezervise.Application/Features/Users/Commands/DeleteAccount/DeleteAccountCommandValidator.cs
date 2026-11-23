using Rezervise.Application.Common.Security;
using FluentValidation;

namespace Rezervise.Application.Features.Users.Commands.DeleteAccount
{
    /// <summary>
    /// Validates the shape of the account deletion request.
    /// </summary>
    public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
    {
        public DeleteAccountCommandValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("Mevcut şifre boş olamaz.")
                .MaximumLength(PasswordPolicy.MaximumLength)
                .WithMessage("Mevcut şifre en fazla 128 karakter olabilir.");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Sebep 500 karakterden uzun olamaz.");
        }
    }
}
