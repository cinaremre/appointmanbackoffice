using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Marketing.Commands.AttributeAppointment
{
    /// <summary>
    /// Randevuyu pazarlama kaynağına atfeder. Bir randevu için tek aktif
    /// atıf kaydı vardır; aynı randevuya ikinci atıf Conflict döner
    /// (DuplicateKey koruması handler'dadır).
    /// </summary>
    public class AttributeAppointmentCommand : IRequest<Result<Guid>>
    {
        public Guid AppointmentId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? CampaignId { get; set; }
        public Guid? TrackingLinkId { get; set; }
        public string? FirstTouchSource { get; set; }
        public string? LastTouchSource { get; set; }
        public EnumAttributionModel AttributionModel { get; set; }
    }
}
