using FluentValidation;

namespace Rezervise.Application.Features.Blacklists.Commands.AddToBlacklist
{
    public class AddToBlacklistCommandValidator : AbstractValidator<AddToBlacklistCommand>
    {
        public AddToBlacklistCommandValidator()
        {
            RuleFor(x => x.RequesterId)
                .NotEmpty().WithMessage("İstek yapan kullanıcı ID boş olamaz.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Kullanıcı ID boş olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty().WithMessage("İşletme ID boş olamaz.");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Sebep 500 karakterden uzun olamaz.");
        }
    }
}