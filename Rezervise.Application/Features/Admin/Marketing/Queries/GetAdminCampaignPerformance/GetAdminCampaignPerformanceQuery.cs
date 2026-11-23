using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminCampaignPerformance
{
    /// <summary>
    /// Tek kampanyanın performans özeti (doküman Modül 15): toplam harcama,
    /// atfedilen randevu ve atfedilen tamamlanan randevu sayıları.
    /// </summary>
    public class GetAdminCampaignPerformanceQuery : IRequest<Result<AdminCampaignPerformanceDto>>
    {
        public Guid CampaignId { get; set; }
    }
}
