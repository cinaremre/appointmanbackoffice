using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Marketing.Commands.RecordCampaignSpend
{
    /// <summary>
    /// Kampanya harcaması kaydeder. Aynı CampaignId+SpendDate+Amount için
    /// çift kayıt engeli YOKTUR; manuel tekrar girilebilir (ilk sürüm).
    /// </summary>
    public class RecordCampaignSpendCommand : IRequest<Result<Guid>>
    {
        public Guid CampaignId { get; set; }
        public DateTime SpendDate { get; set; }
        public decimal Amount { get; set; }

        /// <summary>Varsayılan "TRY".</summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>Varsayılan "manual"; "api" platform senkronlarını belirtir.</summary>
        public string DataSource { get; set; } = "manual";
    }
}
