using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Marketing.Commands.AttributeAppointment
{
    /// <summary>
    /// Randevuyu pazarlama kaynagina atfeder. Bir randevu icin tek aktif
    /// atif kaydi vardir; ayni randevuya ikinci atif Conflict doner
    /// (DuplicateKey korumasi handler'dadir).
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
