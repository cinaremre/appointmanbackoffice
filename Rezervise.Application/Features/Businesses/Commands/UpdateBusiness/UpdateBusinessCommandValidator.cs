using Rezervise.Application.Common.Interfaces;
using FluentValidation;

namespace Rezervise.Application.Features.Businesses.Commands.UpdateBusiness
{
    public class UpdateBusinessCommandValidator : AbstractValidator<UpdateBusinessCommand>
    {
        public UpdateBusinessCommandValidator(ILocalizationService localizer)
        {
            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.BusinessIdRequired"));

            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.OwnerIdRequired"));

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.BusinessNameRequired"))
                .MinimumLength(2)
                .WithMessage(localizer.Get("Validation.BusinessNameMinLength"))
                .MaximumLength(100)
                .WithMessage(localizer.Get("Validation.BusinessNameMaxLength"));

            RuleFor(x => x.BusinessType)
                .IsInEnum()
                .WithMessage(localizer.Get("Validation.BusinessTypeInvalid"));

            RuleFor(x => x.Address)
                .NotNull()
                .WithMessage(localizer.Get("Validation.AddressRequired"));

            RuleFor(x => x.PhoneNumber)
                .NotNull()
                .WithMessage(localizer.Get("Validation.PhoneNumberRequired"));

            RuleFor(x => x.Email)
                .NotNull()
                .WithMessage(localizer.Get("Validation.EmailRequired"));

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage(localizer.Get("Validation.DescriptionMaxLength"));

            RuleFor(x => x.WebsiteUrl)
                .MaximumLength(200)
                .WithMessage(localizer.Get("Validation.WebsiteUrlMaxLength"));
        }
    }
}
