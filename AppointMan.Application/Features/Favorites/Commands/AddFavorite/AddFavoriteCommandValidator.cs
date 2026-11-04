using FluentValidation;

namespace AppointMan.Application.Features.Favorites.Commands.AddFavorite
{
    public class AddFavoriteCommandValidator : AbstractValidator<AddFavoriteCommand>
    {
        public AddFavoriteCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanici ID bos olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("Isletme ID bos olamaz.");

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .WithMessage("Not 500 karakterden uzun olamaz.");
        }
    }
}