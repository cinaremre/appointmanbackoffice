using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Marketing.Commands.CreateCampaign
{
    /// <summary>
    /// Yeni pazarlama kampanyası oluşturur (Instagram, Meta Ads, Google Ads vb.).
    /// </summary>
    public class CreateCampaignCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public EnumCampaignChannel Channel { get; set; }
        public string? ExternalCampaignId { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    }
}
