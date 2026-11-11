using AppointMan.Application.Common.Interfaces;
using FluentValidation;

namespace AppointMan.Application.Features.Users.Commands.VerifyEmail
{
    public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
    {
        public VerifyEmailCommandValidator(ILocalizationService localizer)
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.UserIdRequired"));

            RuleFor(x => x.VerificationCode)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.VerificationCodeRequired"))
                .Length(6)
                .WithMessage(localizer.Get("Validation.VerificationCodeLength"));
        }
    }
}
