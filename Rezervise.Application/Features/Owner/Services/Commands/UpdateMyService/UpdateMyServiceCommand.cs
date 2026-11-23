using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Services.Commands.UpdateMyService
{
    /// <summary>
    /// İşletme sahibinin kendi hizmetini güncellemesi. Sahiplik handler'da
    /// doğrulanır; değişiklik Service entity'nin Set/Update metotlarıyla
    /// yapılır ve denetim kaydına yazılır.
    /// </summary>
    public class UpdateMyServiceCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        public Guid ServiceId { get; set; }

        /// <summary>Yeni hizmet adı; gönderildiyse güncellenir.</summary>
        public string? Name { get; set; }

        /// <summary>Yeni fiyat; gönderildiyse güncellenir.</summary>
        public decimal? Price { get; set; }

        /// <summary>Fiyat para birimi; boş ise mevcut birim korunur.</summary>
        public string? Currency { get; set; }

        /// <summary>Yeni süre (dakika); gönderildiyse güncellenir.</summary>
        public int? DurationMinutes { get; set; }

        /// <summary>Opsiyonel işlem gerekçesi; denetim kaydına yazılır.</summary>
        public string? Reason { get; set; }
    }
}
