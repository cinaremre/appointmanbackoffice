using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Consents.Commands.SetCommunicationConsent
{
    /// <summary>
    /// Musterinin kanal + amac bazinda iletisim iznini belirler (dokuman Modul 16).
    /// Preference upsert edilir, ConsentHistory kaydi eklenir.
    /// Pazarlama izni geri cekilirse kanal tamamen kapatilir (suppression);
    /// izin verilirse mevcut suppression kaldirilir.
    /// </summary>
    public class SetCommunicationConsentCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Musterinin User ID'si.</summary>
        public Guid CustomerId { get; set; }

        public EnumConsentChannel Channel { get; set; }
        public EnumConsentPurpose Purpose { get; set; }
        public bool IsAllowed { get; set; }

        /// <summary>Degisikligin kaynagi: "api" veya "import".</summary>
        public string Source { get; set; } = "api";

        /// <summary>LegalBasis / ispat referansi (form ID, import satir ID vb.).</summary>
        public string? EvidenceReference { get; set; }
    }
}
