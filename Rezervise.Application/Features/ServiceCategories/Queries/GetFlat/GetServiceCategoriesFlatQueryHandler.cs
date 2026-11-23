using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.ServiceCategories.Queries.GetFlat
{
    public class GetServiceCategoriesFlatQueryHandler
        : IRequestHandler<GetServiceCategoriesFlatQuery, Result<List<ServiceCategoryDto>>>
    {
        private readonly IRepository<ServiceCategory> _categoryRepository;

        public GetServiceCategoriesFlatQueryHandler(IRepository<ServiceCategory> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<List<ServiceCategoryDto>>> Handle(
            GetServiceCategoriesFlatQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kategorileri getir.
                //    Translations yüklenmezse GetName() her kategori için "Unknown" döner;
                //    SubCategories/Services yüklenmezse IsLeaf(), GetDepth() ve sayaçlar yanlış olur.
                //    ParentCategory.Translations iç ice yol öldüğü için tek seviye Include yetmez.
                var categories = await _categoryRepository.FindWithIncludesAsync(
                    c => !c.IsDeleted && (!request.OnlyActive || c.IsActive),
                    new[]
                    {
                        nameof(ServiceCategory.Translations),
                        "ParentCategory.Translations",
                        nameof(ServiceCategory.SubCategories),
                        nameof(ServiceCategory.Services)
                    },
                    cancellationToken);

                var categoryList = categories.ToList();

                // 2. Parent filtresi
                if (request.ParentCategoryId.HasValue)
                {
                    categoryList = categoryList
                        .Where(c => c.ParentCategoryId == request.ParentCategoryId.Value)
                        .ToList();
                }

                // 3. Leaf filtresi
                if (request.OnlyLeaves)
                {
                    categoryList = categoryList
                        .Where(c => c.IsLeaf())
                        .ToList();
                }

                // 4. Sırala
                var sortedList = categoryList
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.GetName(request.LanguageCode))
                    .ToList();

                // 5. DTO'ya dönüştür
                var dtos = sortedList.Select(c => new ServiceCategoryDto
                {
                    Id = c.Id,
                    Name = c.GetName(request.LanguageCode),
                    Description = c.GetDescription(request.LanguageCode),
                    IconUrl = c.IconUrl,
                    ColorHex = c.ColorHex,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive,
                    IsLeaf = c.IsLeaf(),
                    ParentCategoryId = c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory?.GetName(request.LanguageCode),
                    SubCategoryCount = c.GetSubCategoryCount(),
                    ServiceCount = c.GetServiceCount(),
                    Depth = c.GetDepth()
                }).ToList();

                return Result<List<ServiceCategoryDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<ServiceCategoryDto>>.Failure(
                    Error.Internal("Kategoriler getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}