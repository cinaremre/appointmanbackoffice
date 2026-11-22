using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.BusinessCategories.Queries.GetById
{
    public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<BusinessCategoryDto>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;

        public GetCategoryByIdQueryHandler(IRepository<BusinessCategory> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<BusinessCategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kategoriyi getir
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);

                // 2. Kategori bulunamadiysa hata dondur
                if (category is null)
                    return Result<BusinessCategoryDto>.Failure(Error.NotFound("Kategori", request.CategoryId.ToString()));

                // 3. DTO'ya donustur
                var dto = new BusinessCategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IconUrl = category.IconUrl,
                    ImageUrl = category.ImageUrl,
                    DisplayOrder = category.DisplayOrder,
                    IsActive = category.IsActive,
                    CorrespondingBusinessType = category.CorrespondingBusinessType,
                    ParentCategoryId = category.ParentCategoryId,
                    ParentCategoryName = category.ParentCategory?.Name,
                    SubCategoryCount = category.GetSubCategoryCount(),
                    BusinessCount = category.GetBusinessCount()
                };

                return Result<BusinessCategoryDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<BusinessCategoryDto>.Failure(Error.Internal("Kategori bilgileri getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}