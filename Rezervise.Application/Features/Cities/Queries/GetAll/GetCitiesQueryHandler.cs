using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Cities.Queries.GetAll
{
    public class GetCitiesQueryHandler : IRequestHandler<GetCitiesQuery, Result<List<CityDto>>>
    {
        private readonly IRepository<City> _cityRepository;

        public GetCitiesQueryHandler(IRepository<City> cityRepository)
        {
            _cityRepository = cityRepository;
        }

        public async Task<Result<List<CityDto>>> Handle(GetCitiesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Tum illeri getir
                var cities = await _cityRepository.GetAllAsync(cancellationToken);

                // 2. Aktif filtreleme
                var filtered = cities.AsEnumerable();
                if (request.OnlyActive.HasValue && request.OnlyActive.Value)
                {
                    filtered = filtered.Where(c => c.IsActive);
                }

                // 3. DTO'ya donustur
                var dtos = filtered
                    .OrderBy(c => c.Name)
                    .Select(c => new CityDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        CountryCode = c.CountryCode,
                        IsActive = c.IsActive
                    })
                    .ToList();

                return Result<List<CityDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<CityDto>>.Failure(
                    Error.Internal("Iller getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
