using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Categories.Queries.GetBusinessCategoryTree
{
    /// <summary>
    /// Isletme kategori agacinin tam listesi (dokuman Modul 9).
    /// Agac duz liste + ParentId ile doner; frontend hiyerarsiyi kendisi kurar.
    /// </summary>
    public class GetBusinessCategoryTreeQuery : IRequest<Result<List<AdminBusinessCategoryTreeNodeDto>>>
    {
        /// <summary>
        /// True ise pasif (arsivlenmis) kategoriler de listeye dahil edilir.
        /// Yonetici agaci default olarak tam gorur.
        /// </summary>
        public bool IncludeInactive { get; set; } = true;
    }

    /// <summary>Duz liste agac dugumu; hiyerarsi ParentId ile kurulur.</summary>
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
