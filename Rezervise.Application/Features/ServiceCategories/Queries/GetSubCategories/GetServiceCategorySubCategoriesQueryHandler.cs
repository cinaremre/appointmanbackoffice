using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.ServiceCategories.Queries.GetSubCategories
{
    public class GetServiceCategorySubCategoriesQueryHandler
        : IRequestHandler<GetServiceCategorySubCategoriesQuery, Result<List<ServiceCategoryDto>>>
    {
        private readonly IRepository<ServiceCategory> _categoryRepository;

        public GetServiceCategorySubCategoriesQueryHandler(IRepository<ServiceCategory> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<List<ServiceCategoryDto>>> Handle(
            GetServiceCategorySubCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Ana kategoriyi kontrol et
                var parentCategory = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (parentCategory is null)
                    return Result<List<ServiceCategoryDto>>.Failure(
                        Error.NotFound("Kategori", request.CategoryId.ToString()));

                // 2. Alt kategorileri getir.
                //    Translations yuklenmezse GetName() "Unknown" doner; SubCategories/Services
                //    yuklenmezse IsLeaf(), GetSubCategoryCount() ve GetServiceCount() yanlis olur.
                var subCategories = await _categoryRepository.FindWithIncludesAsync(
                    c => c.ParentCategoryId == request.CategoryId &&
                         !c.IsDeleted &&
                         (!request.OnlyActive || c.IsActive),
                    new[]
                    {
                        nameof(ServiceCategory.Translations),
                        "ParentCategory.Translations",
                        nameof(ServiceCategory.SubCategories),
                        nameof(ServiceCategory.Services)
                    },
                    cancellationToken);

                // 3. DTO'ya donustur
                var dtos = subCategories
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new ServiceCategoryDto
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
                    })
                    .ToList();

                return Result<List<ServiceCategoryDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<ServiceCategoryDto>>.Failure(
                    Error.Internal("Alt kategoriler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}