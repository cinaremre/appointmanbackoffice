using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Queries.GetAdminFeatureDefinitions
{
    /// <summary>
    /// Platformda tanımlı tüm özellikleri listeler (doküman Modül 13).
    /// Plan ve override uçlarında özellik seçimi bu listeden yapılır.
    /// </summary>
    public class GetAdminFeatureDefinitionsQuery : IRequest<Result<IReadOnlyList<AdminFeatureDefinitionDto>>>
    {
    }

    /// <summary>Özellik tanımının admin görünümü.</summary>
    public class AdminFeatureDefinitionDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
