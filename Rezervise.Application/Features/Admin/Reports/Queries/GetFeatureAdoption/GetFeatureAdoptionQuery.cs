using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Reports.Queries.GetFeatureAdoption
{
    /// <summary>
    /// Özellik benimseme raporu (doküman Modül 18). FeatureDefinition kodu
    /// bazında kaç işletmenin özelliği açık (aktif override) öldüğünü döner.
    /// </summary>
    public class GetFeatureAdoptionQuery : IRequest<Result<List<FeatureAdoptionDto>>>
    {
    }

    /// <summary>Tek bir özelliğin benimseme satırı.</summary>
    public class FeatureAdoptionDto
    {
        /// <summary>Özellik tanım kodu (orn. "WHATSAPP_REMINDER").</summary>
        public string FeatureCode { get; init; } = string.Empty;

        public string FeatureName { get; init; } = string.Empty;

        /// <summary>Özelliği açık işletme sayısı.</summary>
        public int BusinessCount { get; init; }
    }
}
