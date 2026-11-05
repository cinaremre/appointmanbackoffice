using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.ServiceCategories.Queries.GetFlat
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
                // 1. Kategorileri getir
                var categories = await _categoryRepository.FindAsync(
                    c => !c.IsDeleted && (!request.OnlyActive || c.IsActive),
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

                // 4. Sirala
                var sortedList = categoryList
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.GetName(request.LanguageCode))
                    .ToList();

                // 5. DTO'ya donustur
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
                    Error.Internal("Kategoriler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}