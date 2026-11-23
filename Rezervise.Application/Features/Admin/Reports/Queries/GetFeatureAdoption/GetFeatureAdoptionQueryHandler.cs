using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Reports.Queries.GetFeatureAdoption
{
    /// <summary>
    /// Özellik benimseme handler'i (doküman Modül 18). Kayıtların FeatureId
    /// alani FeatureDefinition ile eşleştirilir; aynı işletmenin aynı özellik
    /// için birden çok override'i tek sayılır.
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
                // 1. Özelliği açık (enabled) ve silinmemiş override kayıtları.
                var overrides = await _overrideRepository.FindAsync(
                    o => o.IsEnabled && !o.IsDeleted, cancellationToken);

                // 2. Özellik tanımları FeatureId eşleştirmesi için.
                var features = await _featureRepository.GetAllAsync(cancellationToken);
                var featureById = features.ToDictionary(f => f.Id);

                // 3. Özellik koduna benimseme sayısı: aynı işletme tek sayılır.
                var adoption = overrides
                    .GroupBy(o => o.FeatureId)
                    .Select(g =>
                    {
                        featureById.TryGetValue(g.Key, out var feature);

                        return new FeatureAdoptionDto
                        {
                            FeatureCode = feature?.Code ?? "BILINMEYEN_OZELLIK",
                            FeatureName = feature?.Name ?? "Bilinmeyen Özellik",
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
                    Error.Internal("Özellik benimseme raporu hazırlanırken bir hata oluştu.", ex.Message));
            }
        }
    }
}
