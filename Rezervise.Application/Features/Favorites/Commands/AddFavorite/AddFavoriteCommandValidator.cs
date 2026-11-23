using FluentValidation;

namespace Rezervise.Application.Features.Favorites.Commands.AddFavorite
{
    public class AddFavoriteCommandValidator : AbstractValidator<AddFavoriteCommand>
    {
        public AddFavoriteCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanıcı ID boş olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("İşletme ID boş olamaz.");

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .WithMessage("Not 500 karakterden uzun olamaz.");
        }
    }
}