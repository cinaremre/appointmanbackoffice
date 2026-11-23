using FluentValidation;

namespace Rezervise.Application.Features.BusinessCategories.Commands.CreateCategory
{
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Kategori adı boş olamaz.")
                .MinimumLength(2)
                .WithMessage("Kategori adı en az 2 karakter olmalıdır.")
                .MaximumLength(100)
                .WithMessage("Kategori adı en fazla 100 karakter olmalıdır.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Açıklama en fazla 500 karakter olmalıdır.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Sıralama numarası negatif olamaz.");

            RuleFor(x => x.CorrespondingBusinessType)
                .IsInEnum()
                .When(x => x.CorrespondingBusinessType.HasValue)
                .WithMessage("Geçersiz işletme tipi.");
        }
    }
}