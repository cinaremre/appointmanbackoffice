using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Neighborhoods.Queries.GetByDistrict
{
    /// <summary>
    /// İlçe → locality → mahalle zincirini tek sorguda çözer. Böylece istemci
    /// önce semt listesini, sonra her semt için mahalle listesini çağırmak zorunda
    /// kalmaz. Semti (localityId) boş olan mahalleler de listede kalır.
    /// </summary>
    public class GetNeighborhoodsByDistrictQueryHandler
        : IRequestHandler<GetNeighborhoodsByDistrictQuery, Result<List<NeighborhoodDto>>>
    {
        private readonly IRepository<District> _districtRepository;
        private readonly IRepository<Locality> _localityRepository;
        private readonly IRepository<Neighborhood> _neighborhoodRepository;

        public GetNeighborhoodsByDistrictQueryHandler(
            IRepository<District> districtRepository,
            IRepository<Locality> localityRepository,
            IRepository<Neighborhood> neighborhoodRepository)
        {
            _districtRepository = districtRepository;
            _localityRepository = localityRepository;
            _neighborhoodRepository = neighborhoodRepository;
        }

        public async Task<Result<List<NeighborhoodDto>>> Handle(
            GetNeighborhoodsByDistrictQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request.DistrictId <= 0)
                    return Result<List<NeighborhoodDto>>.Failure(
                        Error.BusinessRule("İlçe ID geçersiz."));

                // 1. İlçeyi doğrula
                var districts = await _districtRepository.FindAsync(
                    d => d.Id == request.DistrictId,
                    cancellationToken);

                if (districts.Count == 0)
                    return Result<List<NeighborhoodDto>>.Failure(
                        Error.NotFound("İlçe", request.DistrictId.ToString()));

                // 2. İlçeye bağlı semtleri getir
                var localities = await _localityRepository.FindAsync(
                    l => l.DistrictId == request.DistrictId,
                    cancellationToken);

                var localityIds = localities.Select(l => l.Id).ToHashSet();

                // 3. Bu semtlerin mahalleleri. Semti olmayan (localityId = 0)
                //    mahalleler de istemcide eksik kalmasın diye listeye dahil edilir.
                var neighborhoods = await _neighborhoodRepository.FindAsync(
                    n => n.LocalityId == 0 || localityIds.Contains(n.LocalityId),
                    cancellationToken);

                // 4. Aktif filtreleme
                var filtered = neighborhoods.AsEnumerable();
                if (request.OnlyActive.HasValue && request.OnlyActive.Value)
                    filtered = filtered.Where(n => n.IsActive);

                // 5. DTO'ya dönüştür. localityId = 0 "boş" anlamına gelir.
                var dtos = filtered
                    .OrderBy(n => n.Name)
                    .Select(n => new NeighborhoodDto
                    {
                        Id = n.Id,
                        Name = n.Name,
                        LocalityId = n.LocalityId == 0 ? null : n.LocalityId,
                        IsActive = n.IsActive
                    })
                    .ToList();

                return Result<List<NeighborhoodDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<NeighborhoodDto>>.Failure(
                    Error.Internal("Mahalleler getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
