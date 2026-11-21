using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Owner.Services.Commands.UpdateMyService
{
    /// <summary>
    /// Isletme sahibinin kendi hizmetini guncellemesi. Sahiplik handler'da
    /// dogrulanir; degisiklik Service entity'nin Set/Update metotlariyla
    /// yapilir ve denetim kaydina yazilir.
    /// </summary>
    public class UpdateMyServiceCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        public Guid ServiceId { get; set; }

        /// <summary>Yeni hizmet adi; gonderildiyse guncellenir.</summary>
        public string? Name { get; set; }

        /// <summary>Yeni fiyat; gonderildiyse guncellenir.</summary>
        public decimal? Price { get; set; }

        /// <summary>Fiyat para birimi; bos ise mevcut birim korunur.</summary>
        public string? Currency { get; set; }

        /// <summary>Yeni sure (dakika); gonderildiyse guncellenir.</summary>
        public int? DurationMinutes { get; set; }

        /// <summary>Opsiyonel islem gerekcesi; denetim kaydina yazilir.</summary>
        public string? Reason { get; set; }
    }
}
