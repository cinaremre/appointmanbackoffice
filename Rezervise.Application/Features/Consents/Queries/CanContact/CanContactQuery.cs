using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Consents.Queries.CanContact
{
    /// <summary>
    /// Belirtilen kanal + amaç için müşteriyetle iletişime geçilabilir mi?
    /// (doküman Modül 16)
    ///
    /// ONEMLI: Pazarlama (Marketing) amaçlı her gönderimden önce bu sorgu
    /// zorunludur. Sonuç false ise gönderim yapılmamalıdır; sorgu atlanarak
    /// gönderilen pazarlama mesajları izin ihlali oluşturur.
    /// </summary>
    public class CanContactQuery : IRequest<Result<bool>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Müşterinin User ID'si.</summary>
        public Guid CustomerId { get; set; }

        public EnumConsentChannel Channel { get; set; }
        public EnumConsentPurpose Purpose { get; set; }
    }
}
