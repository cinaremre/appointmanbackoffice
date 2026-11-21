using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Marketing.Commands.RecordCampaignSpend
{
    /// <summary>
    /// Kampanya harcamasi kaydeder. Ayni CampaignId+SpendDate+Amount icin
    /// cift kayit engeli YOKTUR; manuel tekrar girilebilir (ilk surum).
    /// </summary>
    public class RecordCampaignSpendCommand : IRequest<Result<Guid>>
    {
        public Guid CampaignId { get; set; }
        public DateTime SpendDate { get; set; }
        public decimal Amount { get; set; }

        /// <summary>Varsayilan "TRY".</summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>Varsayilan "manual"; "api" platform senkronlarini belirtir.</summary>
        public string DataSource { get; set; } = "manual";
    }
}
