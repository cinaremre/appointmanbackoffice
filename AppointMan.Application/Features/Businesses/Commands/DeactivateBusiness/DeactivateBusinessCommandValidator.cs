using FluentValidation;

namespace AppointMan.Application.Features.Businesses.Commands.DeactivateBusiness
{
    public class DeactivateBusinessCommandValidator : AbstractValidator<DeactivateBusinessCommand>
    {
        public DeactivateBusinessCommandValidator()
        {
            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("Isletme ID bos olamaz.");

            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Sahip ID bos olamaz.");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Gerekce en fazla 500 karakter olmalidir.");
        }
    }
}