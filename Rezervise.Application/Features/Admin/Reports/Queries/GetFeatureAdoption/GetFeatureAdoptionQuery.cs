using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Reports.Queries.GetFeatureAdoption
{
    /// <summary>
    /// Ozellik benimseme raporu (dokuman Modul 18). FeatureDefinition kodu
    /// bazinda kac isletmenin ozelligi acik (aktif override) oldugunu doner.
    /// </summary>
    public class GetFeatureAdoptionQuery : IRequest<Result<List<FeatureAdoptionDto>>>
    {
    }

    /// <summary>Tek bir ozelligin benimseme satiri.</summary>
    public class FeatureAdoptionDto
    {
        /// <summary>Ozellik tanim kodu (orn. "WHATSAPP_REMINDER").</summary>
        public string FeatureCode { get; init; } = string.Empty;

        public string FeatureName { get; init; } = string.Empty;

        /// <summary>Ozelligi acik isletme sayisi.</summary>
        public int BusinessCount { get; init; }
    }
}
