using FluentValidation;

namespace Rezervise.Application.Features.Businesses.Commands.DeactivateBusiness
{
    public class DeactivateBusinessCommandValidator : AbstractValidator<DeactivateBusinessCommand>
    {
        public DeactivateBusinessCommandValidator()
        {
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