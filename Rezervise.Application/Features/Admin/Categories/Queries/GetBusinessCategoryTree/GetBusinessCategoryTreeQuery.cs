using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Queries.GetBusinessCategoryTree
{
    /// <summary>
    /// İşletme kategori ağacının tam listesi (doküman Modül 9).
    /// Ağaç düz liste + ParentId ile döner; frontend hiyerarşiyi kendisi kurar.
    /// </summary>
    public class GetBusinessCategoryTreeQuery : IRequest<Result<List<AdminBusinessCategoryTreeNodeDto>>>
    {
        /// <summary>
        /// True ise pasif (arşivlenmiş) kategoriler de listeye dahil edilir.
        /// Yönetici ağacı default olarak tam görür.
        /// </summary>
        public bool IncludeInactive { get; set; } = true;
    }

    /// <summary>Düz liste ağaç düğümü; hiyerarşi ParentId ile kurulur.</summary>
    public class AdminBusinessCategoryTreeNodeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int ChildCount { get; set; }
    }
}
