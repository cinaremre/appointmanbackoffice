using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Commands.CreateBusinessCategoryByAdmin
{
    /// <summary>
    /// Platform yöneticisi yeni işletme kategorisi oluşturur (doküman Modül 9).
    /// Aynı ada sahip AKTIF bir kardeş kategori varsa Conflict döner (ilk sürüm).
    /// </summary>
    public class CreateBusinessCategoryByAdminCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public int DisplayOrder { get; set; }
    }
}
