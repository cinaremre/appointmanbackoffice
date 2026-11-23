using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Commands.UpdateBusinessCategoryByAdmin
{
    /// <summary>
    /// Platform yöneticisi kategori ad/sıra/durum alanlarını günceller.
    /// Tüm alanlar opsiyoneldir; yalnızca gönderilenler değişir.
    /// </summary>
    public class UpdateBusinessCategoryByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid CategoryId { get; set; }
        public string? Name { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }
}
