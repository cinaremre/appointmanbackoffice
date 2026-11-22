using FluentValidation;

namespace Rezervise.Application.Features.Blacklists.Commands.AddToBlacklist
{
    public class AddToBlacklistCommandValidator : AbstractValidator<AddToBlacklistCommand>
    {
        public AddToBlacklistCommandValidator()
        {
            RuleFor(x => x.RequesterId)
                .NotEmpty().WithMessage("Istek yapan kullanici ID bos olamaz.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Kullanici ID bos olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty().WithMessage("Isletme ID bos olamaz.");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Sebep 500 karakterden uzun olamaz.");
        }
    }
}