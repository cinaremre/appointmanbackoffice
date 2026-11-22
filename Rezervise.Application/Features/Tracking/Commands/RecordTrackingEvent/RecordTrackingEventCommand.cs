using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Tracking.Commands.RecordTrackingEvent
{
    /// <summary>
    /// Anonim takip olayi kaydi. Takip kodu VEYA slug ile cagrilir.
    /// Kisisel veri tasinmaz; AnonymousSessionId istemciden opsiyoneldir.
    /// </summary>
    public class RecordTrackingEventCommand : IRequest<Result<bool>>
    {
        /// <summary>Takip linkinin kisa kodu. Slug ile birlikte birincil anahtardir.</summary>
        public string? TrackingCode { get; set; }

        /// <summary>Public sayfa slug'i (link kodu yoksa kullanilir).</summary>
        public string? Slug { get; set; }

        public EnumTrackingEventType EventType { get; set; }

        /// <summary>Istemciden opsiyonel anonim oturum kimligi.</summary>
        public Guid? AnonymousSessionId { get; set; }

        /// <summary>Randevu iliskili olaylarda randevu ID'si.</summary>
        public Guid? AppointmentId { get; set; }
    }
}
