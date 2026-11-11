using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Districts.Queries.GetByCity
{
    public class GetDistrictsByCityQueryHandler : IRequestHandler<GetDistrictsByCityQuery, Result<List<DistrictDto>>>
    {
        private readonly IRepository<City> _cityRepository;
        private readonly IRepository<District> _districtRepository;

        public GetDistrictsByCityQueryHandler(
            IRepository<City> cityRepository,
            IRepository<District> districtRepository)
        {
            _cityRepository = cityRepository;
            _districtRepository = districtRepository;
        }

        public async Task<Result<List<DistrictDto>>> Handle(GetDistrictsByCityQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.CityId <= 0)
                    return Result<List<DistrictDto>>.Failure(Error.BusinessRule("Il ID gecersiz."));

                // 1. Ili kontrol et
                var cities = await _cityRepository.FindAsync(c => c.Id == request.CityId, cancellationToken);
                if (cities.Count == 0)
                    return Result<List<DistrictDto>>.Failure(Error.NotFound("Il", request.CityId.ToString()));

                // 2. Ilceleri getir
                var districts = await _districtRepository.FindAsync(
                    d => d.CityId == request.CityId,
                    cancellationToken);

                // 3. Aktif filtreleme
                var filtered = districts.AsEnumerable();
                if (request.OnlyActive.HasValue && request.OnlyActive.Value)
                {
                    filtered = filtered.Where(d => d.IsActive);
                }

                // 4. DTO'ya donustur
                var dtos = filtered
                    .OrderBy(d => d.Name)
                    .Select(d => new DistrictDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        CityId = d.CityId,
                        IsActive = d.IsActive
                    })
                    .ToList();

                return Result<List<DistrictDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<DistrictDto>>.Failure(
                    Error.Internal("Ilceler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
