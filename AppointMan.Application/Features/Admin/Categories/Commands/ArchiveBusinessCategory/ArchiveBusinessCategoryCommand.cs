using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Categories.Commands.ArchiveBusinessCategory
{
    /// <summary>
    /// Kategoriyi arsivler (dokuman Modul 9). Arsivleme = entity'deki pasif
    /// mekanizma (Deactivate): kategori agacta gorunur kalir ama IsActive
    /// false olur. Gerekce (reason) zorunludur ve denetim kaydina yazilir.
    /// Aktif alt kategorisi olan kategori arsivlenemez.
    /// </summary>
    public class ArchiveBusinessCategoryCommand : IRequest<Result<Guid>>
    {
        public Guid CategoryId { get; set; }

        /// <summary>Yoneticinin arsivleme gerekcesi; zorunlu.</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
