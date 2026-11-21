using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Subscriptions.Queries.GetBusinessFeatureOverrides
{
    public class GetBusinessFeatureOverridesQueryHandler
        : IRequestHandler<GetBusinessFeatureOverridesQuery, Result<IReadOnlyList<AdminFeatureOverrideDto>>>
    {
        private readonly IRepository<BusinessFeatureOverride> _overrideRepository;
        private readonly IRepository<FeatureDefinition> _featureDefinitionRepository;

        public GetBusinessFeatureOverridesQueryHandler(
            IRepository<BusinessFeatureOverride> overrideRepository,
            IRepository<FeatureDefinition> featureDefinitionRepository)
        {
            _overrideRepository = overrideRepository;
            _featureDefinitionRepository = featureDefinitionRepository;
        }

        public async Task<Result<IReadOnlyList<AdminFeatureOverrideDto>>> Handle(
            GetBusinessFeatureOverridesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var overrides = await _overrideRepository.FindAsync(
                    o => o.BusinessId == request.BusinessId && !o.IsDeleted,
                    cancellationToken);

                var definitions = await _featureDefinitionRepository.FindAsync(
                    d => !d.IsDeleted,
                    cancellationToken);

                var definitionById = definitions.ToDictionary(d => d.Id);

                var result = overrides
                    .Where(o => definitionById.ContainsKey(o.FeatureId))
                    .OrderByDescending(o => o.ValidFrom)
                    .Select(o => new AdminFeatureOverrideDto
                    {
                        Id = o.Id,
                        FeatureId = o.FeatureId,
                        FeatureCode = definitionById[o.FeatureId].Code,
                        IsEnabled = o.IsEnabled,
                        QuotaOverride = o.QuotaOverride,
                        ValidFrom = o.ValidFrom,
                        ValidUntil = o.ValidUntil,
                        Reason = o.Reason,
                        GrantedBy = o.GrantedBy
                    })
                    .ToList();

                return Result<IReadOnlyList<AdminFeatureOverrideDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<AdminFeatureOverrideDto>>.Failure(
                    Error.Internal("Ozellik override kayitlari getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
