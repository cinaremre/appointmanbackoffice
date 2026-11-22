using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Queries.GetAdminFeatureDefinitions
{
    /// <summary>
    /// Platformda tanimli tum ozellikleri listeler (dokuman Modul 13).
    /// Plan ve override uclarinda ozellik secimi bu listeden yapilir.
    /// </summary>
    public class GetAdminFeatureDefinitionsQuery : IRequest<Result<IReadOnlyList<AdminFeatureDefinitionDto>>>
    {
    }

    /// <summary>Ozellik taniminin admin gorunumu.</summary>
    public class AdminFeatureDefinitionDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
