using FluentValidation;

namespace Rezervise.Application.Features.Services.Queries.SearchServices
{
    public class SearchServicesQueryValidator : AbstractValidator<SearchServicesQuery>
    {
        public SearchServicesQueryValidator()
        {
            RuleFor(x => x.SearchTerm)
                .MaximumLength(100)
                .WithMessage("Arama terimi en fazla 100 karakter olmalıdır.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Sayfa boyutu 1-100 arasında olmalıdır.");
        }
    }
}