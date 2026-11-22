using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Neighborhoods.Queries.GetByLocality
{
    public class GetNeighborhoodsByLocalityQueryHandler : IRequestHandler<GetNeighborhoodsByLocalityQuery, Result<List<NeighborhoodDto>>>
    {
        private readonly IRepository<Locality> _localityRepository;
        private readonly IRepository<Neighborhood> _neighborhoodRepository;

        public GetNeighborhoodsByLocalityQueryHandler(
            IRepository<Locality> localityRepository,
            IRepository<Neighborhood> neighborhoodRepository)
        {
            _localityRepository = localityRepository;
            _neighborhoodRepository = neighborhoodRepository;
        }

        public async Task<Result<List<NeighborhoodDto>>> Handle(GetNeighborhoodsByLocalityQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.LocalityId <= 0)
                    return Result<List<NeighborhoodDto>>.Failure(Error.BusinessRule("Locality ID gecersiz."));

                // 1. Locality kaydini kontrol et
                var localities = await _localityRepository.FindAsync(l => l.Id == request.LocalityId, cancellationToken);
                if (localities.Count == 0)
                    return Result<List<NeighborhoodDto>>.Failure(Error.NotFound("Locality", request.LocalityId.ToString()));

                // 2. Mahalleleri getir
                var neighborhoods = await _neighborhoodRepository.FindAsync(
                    n => n.LocalityId == request.LocalityId,
                    cancellationToken);

                // 3. Aktif filtreleme
                var filtered = neighborhoods.AsEnumerable();
                if (request.OnlyActive.HasValue && request.OnlyActive.Value)
                {
                    filtered = filtered.Where(n => n.IsActive);
                }

                // 4. DTO'ya donustur
                var dtos = filtered
                    .OrderBy(n => n.Name)
                    .Select(n => new NeighborhoodDto
                    {
                        Id = n.Id,
                        Name = n.Name,
                        LocalityId = n.LocalityId,
                        IsActive = n.IsActive
                    })
                    .ToList();

                return Result<List<NeighborhoodDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<NeighborhoodDto>>.Failure(
                    Error.Internal("Mahalleler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
