using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Tracking.Commands.CreateTrackingLink
{
    /// <summary>
    /// İşletme için benzersiz takip kodlu link üretir.
    /// QR görseli frontend tarafında üretilir; burada kod + hedef yol döner.
    /// </summary>
    public class CreateTrackingLinkCommand : IRequest<Result<string>>
    {
        public Guid BusinessId { get; set; }
        public EnumTrackingTargetType TargetType { get; set; }

        /// <summary>Hedef varlık ID'si; Business tipinde boş bırakılabilir.</summary>
        public Guid? TargetId { get; set; }

        /// <summary>"qr", "whatsapp" veya "ınstagram".</summary>
        public string Source { get; set; } = string.Empty;
        public string? Medium { get; set; }
        public string? Campaign { get; set; }

        /// <summary>Linkin yönlendireceği göreli yol.</summary>
        public string DestinationPath { get; set; } = string.Empty;
    }
}
