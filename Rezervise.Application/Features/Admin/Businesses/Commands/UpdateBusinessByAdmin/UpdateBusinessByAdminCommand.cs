using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.UpdateBusinessByAdmin
{
    /// <summary>
    /// Platform yoneticisinin isletme temel bilgilerini guncellemesi
    /// (dokuman Modul 8). Yalnizca bos olmayan alanlar guncellenir;
    /// islem denetim kaydina yazilir.
    /// </summary>
    public class UpdateBusinessByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Yeni isletme adi; bos ise degistirilmez.</summary>
        public string? Name { get; set; }

        /// <summary>Yeni aciklama; null ise degistirilmez.</summary>
        public string? Description { get; set; }

        /// <summary>Yoneticinin islem gerekcesi; denetim kaydina yazilir.</summary>
        public string? Reason { get; set; }
    }
}
