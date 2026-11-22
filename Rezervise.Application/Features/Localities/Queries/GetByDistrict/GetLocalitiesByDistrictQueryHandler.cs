using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Localities.Queries.GetByDistrict
{
    public class GetLocalitiesByDistrictQueryHandler : IRequestHandler<GetLocalitiesByDistrictQuery, Result<List<LocalityDto>>>
    {
        private readonly IRepository<District> _districtRepository;
        private readonly IRepository<Locality> _localityRepository;

        public GetLocalitiesByDistrictQueryHandler(
            IRepository<District> districtRepository,
            IRepository<Locality> localityRepository)
        {
            _districtRepository = districtRepository;
            _localityRepository = localityRepository;
        }

        public async Task<Result<List<LocalityDto>>> Handle(GetLocalitiesByDistrictQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.DistrictId <= 0)
                    return Result<List<LocalityDto>>.Failure(Error.BusinessRule("Ilce ID gecersiz."));

                // 1. Ilceyi kontrol et
                var districts = await _districtRepository.FindAsync(d => d.Id == request.DistrictId, cancellationToken);
                if (districts.Count == 0)
                    return Result<List<LocalityDto>>.Failure(Error.NotFound("Ilce", request.DistrictId.ToString()));

                // 2. Locality kayitlarini getir
                var localities = await _localityRepository.FindAsync(
                    l => l.DistrictId == request.DistrictId,
                    cancellationToken);

                // 3. Aktif filtreleme
                var filtered = localities.AsEnumerable();
                if (request.OnlyActive.HasValue && request.OnlyActive.Value)
                {
                    filtered = filtered.Where(l => l.IsActive);
                }

                // 4. DTO'ya donustur
                var dtos = filtered
                    .OrderBy(l => l.Name)
                    .Select(l => new LocalityDto
                    {
                        Id = l.Id,
                        Name = l.Name,
                        DistrictId = l.DistrictId,
                        IsActive = l.IsActive
                    })
                    .ToList();

                return Result<List<LocalityDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<LocalityDto>>.Failure(
                    Error.Internal("Locality kayitlari getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
