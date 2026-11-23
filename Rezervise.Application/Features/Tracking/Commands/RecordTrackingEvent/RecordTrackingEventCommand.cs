using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Tracking.Commands.RecordTrackingEvent
{
    /// <summary>
    /// Anonim takip olayı kaydı. Takip kodu VEYA slug ile çağrılır.
    /// Kişisel veri taşınmaz; AnonymousSessionId istemciden opsiyoneldir.
    /// </summary>
    public class RecordTrackingEventCommand : IRequest<Result<bool>>
    {
        /// <summary>Takip linkinin kısa kodu. Slug ile birlikte birincil anahtardır.</summary>
        public string? TrackingCode { get; set; }

        /// <summary>Public sayfa slug'i (link kodu yoksa kullanılır).</summary>
        public string? Slug { get; set; }

        public EnumTrackingEventType EventType { get; set; }

        /// <summary>İstemciden opsiyonel anonim oturum kimliği.</summary>
        public Guid? AnonymousSessionId { get; set; }

        /// <summary>Randevu ilişkili olaylarda randevu ID'si.</summary>
        public Guid? AppointmentId { get; set; }
    }
}
