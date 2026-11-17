using FluentValidation;

namespace AppointMan.Application.Features.Autocomplete.Queries.Search
{
    /// <summary>
    /// Validates <see cref="AutocompleteQuery"/>: rejects unsupported source
    /// types, enforces the 3 character minimum and caps the page size.
    /// </summary>
    public class AutocompleteQueryValidator : AbstractValidator<AutocompleteQuery>
    {
        // Karsilastirma buyuk/kucuk harf duyarsiz olsun diye OrdinalIgnoreCase kullanilir.
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
                .WithMessage("Arama tipi bos olamaz.")
                .Must(type => SupportedTypes.Contains((type ?? string.Empty).Trim()))
                .WithMessage("Desteklenmeyen arama tipi.");

            RuleFor(x => x.Query)
                .NotEmpty()
                .WithMessage("Arama terimi bos olamaz.")
                .MinimumLength(3)
                .WithMessage("Arama terimi en az 3 karakter olmalidir.")
                .MaximumLength(100)
                .WithMessage("Arama terimi en fazla 100 karakter olabilir.");

            RuleFor(x => x.Limit)
                .InclusiveBetween(1, 25)
                .WithMessage("Sonuc sayisi 1-25 arasinda olmalidir.");
        }
    }
}
