using FluentValidation;

namespace Rezervise.Application.Features.Businesses.Queries.SearchBusinesses
{
    public class SearchBusinessesQueryValidator : AbstractValidator<SearchBusinessesQuery>
    {
        public SearchBusinessesQueryValidator()
        {
            RuleFor(x => x.City)
                .MaximumLength(100)
                .WithMessage("Şehir adı en fazla 100 karakter olabilir.");

            RuleFor(x => x.CityId)
                .GreaterThan(0)
                .When(x => x.CityId.HasValue)
                .WithMessage("Şehir ID geçersiz.");

            RuleFor(x => x.DistrictId)
                .GreaterThan(0)
                .When(x => x.DistrictId.HasValue)
                .WithMessage("İlçe ID geçersiz.");

            RuleFor(x => x.LocalityId)
                .GreaterThan(0)
                .WithMessage("Locality ID geçersiz.");

            RuleFor(x => x.NeighborhoodId)
                .GreaterThan(0)
                .WithMessage("Mahalle ID geçersiz.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Sayfa boyutu 1-100 arasında olmalıdır.");
        }
    }
}