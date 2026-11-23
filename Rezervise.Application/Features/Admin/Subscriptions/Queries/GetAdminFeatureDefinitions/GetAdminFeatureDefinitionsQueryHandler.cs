using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Queries.GetAdminFeatureDefinitions
{
    public class GetAdminFeatureDefinitionsQueryHandler
        : IRequestHandler<GetAdminFeatureDefinitionsQuery, Result<IReadOnlyList<AdminFeatureDefinitionDto>>>
    {
        private readonly IRepository<FeatureDefinition> _featureDefinitionRepository;

        public GetAdminFeatureDefinitionsQueryHandler(
            IRepository<FeatureDefinition> featureDefinitionRepository)
        {
            _featureDefinitionRepository = featureDefinitionRepository;
        }

        public async Task<Result<IReadOnlyList<AdminFeatureDefinitionDto>>> Handle(
            GetAdminFeatureDefinitionsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var definitions = await _featureDefinitionRepository.FindAsync(
                    d => !d.IsDeleted,
                    cancellationToken);

                var result = definitions
                    .OrderBy(d => d.Code, StringComparer.Ordinal)
                    .Select(d => new AdminFeatureDefinitionDto
                    {
                        Id = d.Id,
                        Code = d.Code,
                        Name = d.Name,
                        Description = d.Description,
                        IsActive = d.IsActive
                    })
                    .ToList();

                return Result<IReadOnlyList<AdminFeatureDefinitionDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<AdminFeatureDefinitionDto>>.Failure(
                    Error.Internal("Özellik tanımları getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
