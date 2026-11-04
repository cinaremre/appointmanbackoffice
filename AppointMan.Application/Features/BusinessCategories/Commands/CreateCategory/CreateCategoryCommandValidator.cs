using FluentValidation;

namespace AppointMan.Application.Features.BusinessCategories.Commands.CreateCategory
{
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Kategori adi bos olamaz.")
                .MinimumLength(2)
                .WithMessage("Kategori adi en az 2 karakter olmalidir.")
                .MaximumLength(100)
                .WithMessage("Kategori adi en fazla 100 karakter olmalidir.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Aciklama en fazla 500 karakter olmalidir.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Siralama numarasi negatif olamaz.");

            RuleFor(x => x.CorrespondingBusinessType)
                .IsInEnum()
                .When(x => x.CorrespondingBusinessType.HasValue)
                .WithMessage("Gecersiz isletme tipi.");
        }
    }
}