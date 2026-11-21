using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Reports.Queries.GetFeatureAdoption
{
    /// <summary>
    /// Ozellik benimseme handler'i (dokuman Modul 18). Kayitlarin FeatureId
    /// alani FeatureDefinition ile eslestirilir; ayni isletmenin ayni ozellik
    /// icin birden cok override'i tek sayilir.
    /// </summary>
    public class GetFeatureAdoptionQueryHandler : IRequestHandler<GetFeatureAdoptionQuery, Result<List<FeatureAdoptionDto>>>
    {
        private readonly IRepository<BusinessFeatureOverride> _overrideRepository;
        private readonly IRepository<FeatureDefinition> _featureRepository;

        public GetFeatureAdoptionQueryHandler(
            IRepository<BusinessFeatureOverride> overrideRepository,
            IRepository<FeatureDefinition> featureRepository)
        {
            _overrideRepository = overrideRepository;
            _featureRepository = featureRepository;
        }

        public async Task<Result<List<FeatureAdoptionDto>>> Handle(GetFeatureAdoptionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Ozelligi acik (enabled) ve silinmemis override kayitlari.
                var overrides = await _overrideRepository.FindAsync(
                    o => o.IsEnabled && !o.IsDeleted, cancellationToken);

                // 2. Ozellik tanimlari FeatureId eslestirmesi icin.
                var features = await _featureRepository.GetAllAsync(cancellationToken);
                var featureById = features.ToDictionary(f => f.Id);

                // 3. Ozellik koduna benimseme sayisi: ayni isletme tek sayilir.
                var adoption = overrides
                    .GroupBy(o => o.FeatureId)
                    .Select(g =>
                    {
                        featureById.TryGetValue(g.Key, out var feature);

                        return new FeatureAdoptionDto
                        {
                            FeatureCode = feature?.Code ?? "BILINMEYEN_OZELLIK",
                            FeatureName = feature?.Name ?? "Bilinmeyen Ozellik",
                            BusinessCount = g.Select(o => o.BusinessId).Distinct().Count()
                        };
                    })
                    .OrderByDescending(f => f.BusinessCount)
                    .ThenBy(f => f.FeatureCode)
                    .ToList();

                return Result<List<FeatureAdoptionDto>>.Success(adoption);
            }
            catch (Exception ex)
            {
                return Result<List<FeatureAdoptionDto>>.Failure(
                    Error.Internal("Ozellik benimseme raporu hazirlanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
