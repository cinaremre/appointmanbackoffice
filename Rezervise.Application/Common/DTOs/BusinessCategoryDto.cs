using Rezervise.Domain.Enums;

namespace Rezervise.Application.Common.DTOs
{
    public class BusinessCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public EnumBusinessType? CorrespondingBusinessType { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
        public int SubCategoryCount { get; set; }
        public int BusinessCount { get; set; }

        /// <summary>
        /// Alt kategoriler. Yalnızca ana kategorilerde doludur; her alt kategori
        /// kendi sayaçlarını taşır.
        /// </summary>
        public List<BusinessCategoryDto> SubCategories { get; set; } = new();
    }
}