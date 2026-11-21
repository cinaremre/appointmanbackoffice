using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Tracking.Commands.CreateTrackingLink
{
    /// <summary>
    /// Isletme icin benzersiz takip kodlu link uretir.
    /// QR gorseli frontend tarafinda uretilir; burada kod + hedef yol doner.
    /// </summary>
    public class CreateTrackingLinkCommand : IRequest<Result<string>>
    {
        public Guid BusinessId { get; set; }
        public EnumTrackingTargetType TargetType { get; set; }

        /// <summary>Hedef varlik ID'si; Business tipinde bos birakilabilir.</summary>
        public Guid? TargetId { get; set; }

        /// <summary>"qr", "whatsapp" veya "instagram".</summary>
        public string Source { get; set; } = string.Empty;
        public string? Medium { get; set; }
        public string? Campaign { get; set; }

        /// <summary>Linkin yonlendirecegi goreli yol.</summary>
        public string DestinationPath { get; set; } = string.Empty;
    }
}
