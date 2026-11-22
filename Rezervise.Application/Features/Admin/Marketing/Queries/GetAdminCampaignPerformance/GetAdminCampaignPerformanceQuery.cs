using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminCampaignPerformance
{
    /// <summary>
    /// Tek kampanyanin performans ozeti (dokuman Modul 15): toplam harcama,
    /// atfedilen randevu ve atfedilen tamamlanan randevu sayilari.
    /// </summary>
    public class GetAdminCampaignPerformanceQuery : IRequest<Result<AdminCampaignPerformanceDto>>
    {
        public Guid CampaignId { get; set; }
    }
}
