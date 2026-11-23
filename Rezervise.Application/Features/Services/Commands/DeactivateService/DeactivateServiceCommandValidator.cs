using FluentValidation;

namespace Rezervise.Application.Features.Services.Commands.DeactivateService
{
    public class DeactivateServiceCommandValidator : AbstractValidator<DeactivateServiceCommand>
    {
        public DeactivateServiceCommandValidator()
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

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Gerekçe en fazla 500 karakter olmalıdır.");
        }
    }
}