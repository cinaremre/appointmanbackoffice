using FluentValidation;

namespace AppointMan.Application.Features.Businesses.Queries.SearchBusinessesByService
{
    public class SearchBusinessesByServiceQueryValidator : AbstractValidator<SearchBusinessesByServiceQuery>
    {
        public SearchBusinessesByServiceQueryValidator()
        {
            RuleFor(x => x.CityId)
                .GreaterThan(0)
                .When(x => x.CityId.HasValue)
                .WithMessage("Sehir ID gecersiz.");

            RuleFor(x => x.DistrictId)
                .GreaterThan(0)
                .When(x => x.DistrictId.HasValue)
                .WithMessage("Ilce ID gecersiz.");

            RuleFor(x => x.LocalityId)
                .GreaterThan(0)
                .WithMessage("Locality ID gecersiz.");

            RuleFor(x => x.NeighborhoodId)
                .GreaterThan(0)
                .WithMessage("Mahalle ID gecersiz.");

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