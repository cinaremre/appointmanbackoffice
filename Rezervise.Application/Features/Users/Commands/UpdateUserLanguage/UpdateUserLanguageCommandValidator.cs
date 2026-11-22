using Rezervise.Application.Common.Interfaces;
using FluentValidation;

namespace Rezervise.Application.Features.Users.Commands.UpdateUserLanguage
{
    public class UpdateUserLanguageCommandValidator : AbstractValidator<UpdateUserLanguageCommand>
    {
        public UpdateUserLanguageCommandValidator(ILocalizationService localizer)
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.UserIdRequired"));

            RuleFor(x => x.LanguageCode)
                .NotEmpty()
                .WithMessage(localizer.Get("User.LanguageCodeRequired"))
                .Must(code => string.IsNullOrWhiteSpace(code)
                              || code.Trim().ToLowerInvariant() is "tr" or "en")
                .WithMessage(localizer.Get("Validation.LanguageCodeUnsupported"));
        }
    }
}
