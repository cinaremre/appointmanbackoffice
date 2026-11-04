using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.BusinessCategories.Queries.GetSubCategories
{
    public class GetSubCategoriesQueryHandler : IRequestHandler<GetSubCategoriesQuery, Result<List<BusinessCategoryDto>>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;

        public GetSubCategoriesQueryHandler(IRepository<BusinessCategory> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<List<BusinessCategoryDto>>> Handle(GetSubCategoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Ana kategoriyi kontrol et
                var parentCategory = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (parentCategory is null)
                    return Result<List<BusinessCategoryDto>>.Failure(Error.NotFound("Kategori", request.CategoryId.ToString()));

                // 2. Alt kategorileri getir
                var subCategories = await _categoryRepository.FindAsync(
                    c => c.ParentCategoryId == request.CategoryId && !c.IsDeleted,
                    cancellationToken);

                // 3. DTO'ya donustur
                var dtos = subCategories.Select(c => new BusinessCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IconUrl = c.IconUrl,
                    ImageUrl = c.ImageUrl,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive,
                    CorrespondingBusinessType = c.CorrespondingBusinessType,
                    ParentCategoryId = c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory?.Name,
                    SubCategoryCount = c.GetSubCategoryCount(),
                    BusinessCount = c.GetBusinessCount()
                }).ToList();

                return Result<List<BusinessCategoryDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<BusinessCategoryDto>>.Failure(
                    Error.Internal("Alt kategoriler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}