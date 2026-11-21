using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Categories.Commands.UpdateBusinessCategoryByAdmin
{
    /// <summary>
    /// Platform yoneticisi kategori ad/sira/durum alanlarini gunceller.
    /// Tum alanlar opsiyoneldir; yalnizca gonderilenler degisir.
    /// </summary>
    public class UpdateBusinessCategoryByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid CategoryId { get; set; }
        public string? Name { get; set; }
        public int? DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }
}
