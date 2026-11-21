using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Categories.Commands.CreateBusinessCategoryByAdmin
{
    /// <summary>
    /// Platform yoneticisi yeni isletme kategorisi olusturur (dokuman Modul 9).
    /// Ayni ada sahip AKTIF bir kardes kategori varsa Conflict doner (ilk surum).
    /// </summary>
    public class CreateBusinessCategoryByAdminCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public int DisplayOrder { get; set; }
    }
}
