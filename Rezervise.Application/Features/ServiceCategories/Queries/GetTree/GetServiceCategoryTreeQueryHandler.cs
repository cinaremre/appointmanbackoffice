using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.ServiceCategories.Queries.GetTree
{
    public class GetServiceCategoryTreeQueryHandler
        : IRequestHandler<GetServiceCategoryTreeQuery, Result<List<ServiceCategoryDto>>>
    {
        private readonly IRepository<ServiceCategory> _categoryRepository;

        public GetServiceCategoryTreeQueryHandler(IRepository<ServiceCategory> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<List<ServiceCategoryDto>>> Handle(
            GetServiceCategoryTreeQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Tüm kategorileri getir.
                //    Translations yüklenmezse GetName() "Unknown" döner; SubCategories/Services
                //    yüklenmezse IsLeaf(), GetSubCategoryCount() ve GetServiceCount() yanlış olur.
                //    Tüm kategoriler tek sorguda geldiği için EF fixup hiyerarşiyi de bağlar.
                var allCategories = await _categoryRepository.FindWithIncludesAsync(
                    c => !c.IsDeleted && (!request.OnlyActive || c.IsActive),
                    new[]
                    {
                        nameof(ServiceCategory.Translations),
                        "ParentCategory.Translations",
                        nameof(ServiceCategory.SubCategories),
                        nameof(ServiceCategory.Services)
                    },
                    cancellationToken);

                var categoryList = allCategories.ToList();

                // 2. Root kategorileri bul
                var rootCategories = categoryList
                    .Where(c => c.ParentCategoryId == null)
                    .OrderBy(c => c.DisplayOrder)
                    .ToList();

                // 3. Her root için recursive tree oluştur
                var tree = rootCategories
                    .Select(c => BuildTree(c, categoryList, request.LanguageCode))
                    .ToList();

                return Result<List<ServiceCategoryDto>>.Success(tree);
            }
            catch (Exception ex)
            {
                return Result<List<ServiceCategoryDto>>.Failure(
                    Error.Internal("Kategori ağacı getirilirken bir hata oluştu.", ex.Message));
            }
        }

        private ServiceCategoryDto BuildTree(
            ServiceCategory category,
            List<ServiceCategory> allCategories,
            string languageCode)
        {
            var dto = new ServiceCategoryDto
            {
                Id = category.Id,
                Name = category.GetName(languageCode),
                Description = category.GetDescription(languageCode),
                IconUrl = category.IconUrl,
                ColorHex = category.ColorHex,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                IsLeaf = category.IsLeaf(),
                ParentCategoryId = category.ParentCategoryId,
                ParentCategoryName = category.ParentCategory?.GetName(languageCode),
                SubCategoryCount = category.GetSubCategoryCount(),
                ServiceCount = category.GetServiceCount(),
                Depth = category.GetDepth(),
                SubCategories = new List<ServiceCategoryDto>()
            };

            // Alt kategorileri recursive olarak ekle
            var children = allCategories
                .Where(c => c.ParentCategoryId == category.Id)
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            foreach (var child in children)
            {
                dto.SubCategories.Add(BuildTree(child, allCategories, languageCode));
            }

            return dto;
        }
    }
}