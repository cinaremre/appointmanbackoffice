using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Consents.Queries.CanContact
{
    /// <summary>
    /// Belirtilen kanal + amac icin musteriyetle iletisime gecilabilir mi?
    /// (dokuman Modul 16)
    ///
    /// ONEMLI: Pazarlama (Marketing) amacli her gonderimden once bu sorgu
    /// zorunludur. Sonuc false ise gonderim yapilmamalidir; sorgu atlanarak
    /// gonderilen pazarlama mesajlari izin ihlali olusturur.
    /// </summary>
    public class CanContactQuery : IRequest<Result<bool>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Musterinin User ID'si.</summary>
        public Guid CustomerId { get; set; }

        public EnumConsentChannel Channel { get; set; }
        public EnumConsentPurpose Purpose { get; set; }
    }
}
