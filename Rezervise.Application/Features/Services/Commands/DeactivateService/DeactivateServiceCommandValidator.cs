using FluentValidation;

namespace Rezervise.Application.Features.Services.Commands.DeactivateService
{
    public class DeactivateServiceCommandValidator : AbstractValidator<DeactivateServiceCommand>
    {
        public DeactivateServiceCommandValidator()
        {
            RuleFor(x => x.ServiceId)
                .NotEmpty()
                .WithMessage("Hizmet ID bos olamaz.");

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