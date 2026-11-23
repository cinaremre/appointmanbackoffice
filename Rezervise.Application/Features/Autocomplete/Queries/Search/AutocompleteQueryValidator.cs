using FluentValidation;

namespace Rezervise.Application.Features.Autocomplete.Queries.Search
{
    /// <summary>
    /// Validates <see cref="AutocompleteQuery"/>: rejects unsupported source
    /// types, enforces the 3 character minimum and caps the page size.
    /// </summary>
    public class AutocompleteQueryValidator : AbstractValidator<AutocompleteQuery>
    {
        // Karşılaştırma büyük/küçük harf duyarsız olsun diye OrdinalIgnoreCase kullanılır.
        private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "city",
            "district",
            "locality",
            "neighborhood",
            "businessCategory",
            "serviceCategory"
        };

        public AutocompleteQueryValidator()
        {
            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("Arama tipi boş olamaz.")
                .Must(type => SupportedTypes.Contains((type ?? string.Empty).Trim()))
                .WithMessage("Desteklenmeyen arama tipi.");

            RuleFor(x => x.Query)
                .NotEmpty()
                .WithMessage("Arama terimi boş olamaz.")
                .MinimumLength(3)
                .WithMessage("Arama terimi en az 3 karakter olmalıdır.")
                .MaximumLength(100)
                .WithMessage("Arama terimi en fazla 100 karakter olabilir.");

            RuleFor(x => x.Limit)
                .InclusiveBetween(1, 25)
                .WithMessage("Sonuç sayısı 1-25 arasında olmalıdır.");
        }
    }
}
