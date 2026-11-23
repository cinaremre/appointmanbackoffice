using FluentValidation;

namespace Rezervise.Application.Features.Blacklists.Commands.RemoveFromBlacklist
{
    public class RemoveFromBlacklistCommandValidator : AbstractValidator<RemoveFromBlacklistCommand>
    {
        public RemoveFromBlacklistCommandValidator()
        {
            RuleFor(x => x.BlacklistEntryId)
                .NotEmpty().WithMessage("Kara liste kaydı ID boş olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty().WithMessage("İşletme ID boş olamaz.");

            RuleFor(x => x.RequesterId)
                .NotEmpty().WithMessage("İstek yapan kullanıcı ID boş olamaz.");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Sebep 500 karakterden uzun olamaz.");
        }
    }
}