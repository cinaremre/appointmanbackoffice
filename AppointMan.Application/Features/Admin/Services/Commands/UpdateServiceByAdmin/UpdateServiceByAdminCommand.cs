using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Services.Commands.UpdateServiceByAdmin
{
    /// <summary>
    /// Platform yoneticisi isletme sahibi adina degil, platform adina
    /// hizmet bilgisi gunceller (dokuman Modul 10). Tum alanlar opsiyoneldir.
    /// </summary>
    public class UpdateServiceByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid ServiceId { get; set; }
        public string? Name { get; set; }

        /// <summary>Yeni fiyat tutari; Currency belirtilmezse TRY kullanilir.</summary>
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int? DurationMinutes { get; set; }
    }
}
