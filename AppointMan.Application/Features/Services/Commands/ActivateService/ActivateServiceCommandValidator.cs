using FluentValidation;

namespace AppointMan.Application.Features.Services.Commands.ActivateService
{
    public class ActivateServiceCommandValidator : AbstractValidator<ActivateServiceCommand>
    {
        public ActivateServiceCommandValidator()
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
        }
    }
}