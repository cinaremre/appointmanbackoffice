using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Commands.ArchiveBusinessCategory
{
    /// <summary>
    /// Kategoriyi arşivler (doküman Modül 9). Arşivleme = entity'deki pasif
    /// mekanizma (Deactivate): kategori ağaçta görünür kalır ama IsActive
    /// false olur. Gerekçe (reason) zorunludur ve denetim kaydına yazılır.
    /// Aktif alt kategorisi olan kategori arşivlenemez.
    /// </summary>
    public class ArchiveBusinessCategoryCommand : IRequest<Result<Guid>>
    {
        public Guid CategoryId { get; set; }

        /// <summary>Yöneticinin arşivleme gerekçesi; zorunlu.</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
