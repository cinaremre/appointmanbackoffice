using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Subscriptions.Queries.GetBusinessFeatureUsage
{
    public class GetBusinessFeatureUsageQueryHandler
        : IRequestHandler<GetBusinessFeatureUsageQuery, Result<IReadOnlyList<AdminFeatureUsageDto>>>
    {
        private readonly IRepository<FeatureUsage> _featureUsageRepository;
        private readonly IRepository<FeatureDefinition> _featureDefinitionRepository;

        public GetBusinessFeatureUsageQueryHandler(
            IRepository<FeatureUsage> featureUsageRepository,
            IRepository<FeatureDefinition> featureDefinitionRepository)
        {
            _featureUsageRepository = featureUsageRepository;
            _featureDefinitionRepository = featureDefinitionRepository;
        }

        public async Task<Result<IReadOnlyList<AdminFeatureUsageDto>>> Handle(
            GetBusinessFeatureUsageQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Su anki ay dahil son uc donem ("YYYY-MM").
                var now = DateTime.UtcNow;
                var periods = new[]
                {
                    now.ToString("yyyy-MM"),
                    now.AddMonths(-1).ToString("yyyy-MM"),
                    now.AddMonths(-2).ToString("yyyy-MM")
                };

                // 2. Donem icindeki kullanim kayitlari ve ozellik tanimlari.
                var usages = await _featureUsageRepository.FindAsync(
                    u => u.BusinessId == request.BusinessId
                        && periods.Contains(u.UsagePeriod)
                        && !u.IsDeleted,
                    cancellationToken);

                var definitions = await _featureDefinitionRepository.FindAsync(
                    d => !d.IsDeleted,
                    cancellationToken);

                var definitionById = definitions.ToDictionary(d => d.Id);

                // 3. Ozellik ve donem bazinda sirali liste.
                var result = usages
                    .Where(u => definitionById.ContainsKey(u.FeatureId))
                    .Select(u => new AdminFeatureUsageDto
                    {
                        FeatureId = u.FeatureId,
                        FeatureCode = definitionById[u.FeatureId].Code,
                        FeatureName = definitionById[u.FeatureId].Name,
                        UsagePeriod = u.UsagePeriod,
                        UsedQuantity = u.UsedQuantity
                    })
                    .OrderByDescending(u => u.UsagePeriod, StringComparer.Ordinal)
                    .ThenBy(u => u.FeatureCode, StringComparer.Ordinal)
                    .ToList();

                return Result<IReadOnlyList<AdminFeatureUsageDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<AdminFeatureUsageDto>>.Failure(
                    Error.Internal("Ozellik kullanimlari getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
