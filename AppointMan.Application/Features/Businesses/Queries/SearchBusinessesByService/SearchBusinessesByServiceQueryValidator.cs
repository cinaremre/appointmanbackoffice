using FluentValidation;

namespace AppointMan.Application.Features.Businesses.Queries.SearchBusinessesByService
{
    public class SearchBusinessesByServiceQueryValidator : AbstractValidator<SearchBusinessesByServiceQuery>
    {
        public SearchBusinessesByServiceQueryValidator()
        {
            RuleFor(x => x.City)
                .NotEmpty()
                .WithMessage("Sehir bos olamaz.")
                .MaximumLength(50)
                .WithMessage("Sehir en fazla 50 karakter olmalidir.");

            RuleFor(x => x.District)
                .MaximumLength(50)
                .WithMessage("Ilce en fazla 50 karakter olmalidir.");

            RuleFor(x => x.ServiceSearchTerm)
                .MaximumLength(100)
                .WithMessage("Arama terimi en fazla 100 karakter olmalidir.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Sayfa numarasi 1 veya daha buyuk olmalidir.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Sayfa boyutu 1-100 arasinda olmalidir.");
        }
    }
}