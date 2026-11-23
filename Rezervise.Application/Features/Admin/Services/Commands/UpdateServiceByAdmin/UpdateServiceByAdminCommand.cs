using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Services.Commands.UpdateServiceByAdmin
{
    /// <summary>
    /// Platform yöneticisi işletme sahibi adına değil, platform adına
    /// hizmet bilgisi günceller (doküman Modül 10). Tüm alanlar opsiyoneldir.
    /// </summary>
    public class UpdateServiceByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid ServiceId { get; set; }
        public string? Name { get; set; }

        /// <summary>Yeni fiyat tutarı; Currency belirtilmezse TRY kullanılır.</summary>
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int? DurationMinutes { get; set; }
    }
}
