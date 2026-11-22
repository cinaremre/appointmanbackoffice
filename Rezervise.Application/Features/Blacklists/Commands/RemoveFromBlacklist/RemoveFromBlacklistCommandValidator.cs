using FluentValidation;

namespace Rezervise.Application.Features.Blacklists.Commands.RemoveFromBlacklist
{
    public class RemoveFromBlacklistCommandValidator : AbstractValidator<RemoveFromBlacklistCommand>
    {
        public RemoveFromBlacklistCommandValidator()
        {
            RuleFor(x => x.BlacklistEntryId)
                .NotEmpty().WithMessage("Kara liste kaydi ID bos olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty().WithMessage("Isletme ID bos olamaz.");

            RuleFor(x => x.RequesterId)
                .NotEmpty().WithMessage("Istek yapan kullanici ID bos olamaz.");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Sebep 500 karakterden uzun olamaz.");
        }
    }
}