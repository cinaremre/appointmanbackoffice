using FluentValidation;

namespace Rezervise.Application.Features.Services.Commands.ActivateService
{
    public class ActivateServiceCommandValidator : AbstractValidator<ActivateServiceCommand>
    {
        public ActivateServiceCommandValidator()
        {
            RuleFor(x => x.ServiceId)
                .NotEmpty()
                .WithMessage("Hizmet ID boş olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("İşletme ID boş olamaz.");

            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Sahip ID boş olamaz.");
        }
    }
}