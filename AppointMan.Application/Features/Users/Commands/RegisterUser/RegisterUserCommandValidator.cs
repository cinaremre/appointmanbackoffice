using AppointMan.Application.Common.Interfaces;
using FluentValidation;

namespace AppointMan.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator(ILocalizationService localizer)
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.FirstNameRequired"))
                .MinimumLength(2)
                .WithMessage(localizer.Get("Validation.FirstNameMinLength"))
                .MaximumLength(50)
                .WithMessage(localizer.Get("Validation.FirstNameMaxLength"));

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.LastNameRequired"))
                .MinimumLength(2)
                .WithMessage(localizer.Get("Validation.LastNameMinLength"))
                .MaximumLength(50)
                .WithMessage(localizer.Get("Validation.LastNameMaxLength"));

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.EmailRequired"))
                .EmailAddress()
                .WithMessage(localizer.Get("Validation.EmailInvalid"));

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.PhoneNumberRequired"))
                .MinimumLength(10)
                .WithMessage(localizer.Get("Validation.PhoneNumberMinLength"))
                .MaximumLength(15)
                .WithMessage(localizer.Get("Validation.PhoneNumberMaxLength"));

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.PasswordRequired"))
                .MinimumLength(6)
                .WithMessage(localizer.Get("Validation.PasswordMinLength"))
                .MaximumLength(50)
                .WithMessage(localizer.Get("Validation.PasswordMaxLength"));

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage(localizer.Get("Validation.RoleInvalid"));

            RuleFor(x => x.DateOfBirth)
                .Must(date => !date.HasValue || date.Value < DateTime.UtcNow.AddYears(-18))
                .WithMessage(localizer.Get("Validation.DateOfBirthMinAge"));

            RuleFor(x => x.CityId)
                .GreaterThan(0)
                .When(x => x.CityId.HasValue)
                .WithMessage(localizer.Get("Address.CityIdInvalid"));

            RuleFor(x => x.DistrictId)
                .GreaterThan(0)
                .When(x => x.DistrictId.HasValue)
                .WithMessage(localizer.Get("Address.DistrictIdInvalid"));

            // Dil kodu istege baglidir; bos ise domain varsayilani ("tr") uygulanir.
            RuleFor(x => x.LanguageCode)
                .Must(code => string.IsNullOrWhiteSpace(code)
                              || code.Trim().ToLowerInvariant() is "tr" or "en")
                .WithMessage(localizer.Get("Validation.LanguageCodeUnsupported"));
        }
    }
}
