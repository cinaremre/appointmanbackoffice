using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Consents.Commands.SetCommunicationConsent
{
    /// <summary>
    /// Müşterinin kanal + amaç bazında iletişim iznini belirler (doküman Modül 16).
    /// Preference upsert edilir, ConsentHistory kaydı eklenir.
    /// Pazarlama izni geri çekilirse kanal tamamen kapatılır (suppression);
    /// izin verilirse mevcut suppression kaldırılır.
    /// </summary>
    public class SetCommunicationConsentCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Müşterinin User ID'si.</summary>
        public Guid CustomerId { get; set; }

        public EnumConsentChannel Channel { get; set; }
        public EnumConsentPurpose Purpose { get; set; }
        public bool IsAllowed { get; set; }

        /// <summary>Değişikliğin kaynağı: "api" veya "import".</summary>
        public string Source { get; set; } = "api";

        /// <summary>LegalBasis / ispat referansı (form ID, import satır ID vb.).</summary>
        public string? EvidenceReference { get; set; }
    }
}
