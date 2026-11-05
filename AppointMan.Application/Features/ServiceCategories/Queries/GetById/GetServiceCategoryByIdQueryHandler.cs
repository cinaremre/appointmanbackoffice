using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.ServiceCategories.Queries.GetById
{
    public class GetServiceCategoryByIdQueryHandler
        : IRequestHandler<GetServiceCategoryByIdQuery, Result<ServiceCategoryDto>>
    {
        private readonly IRepository<ServiceCategory> _categoryRepository;

        public GetServiceCategoryByIdQueryHandler(IRepository<ServiceCategory> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<ServiceCategoryDto>> Handle(
            GetServiceCategoryByIdQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (category is null)
                    return Result<ServiceCategoryDto>.Failure(
                        Error.NotFound("Kategori", request.CategoryId.ToString()));

                var dto = new ServiceCategoryDto
                {
                    Id = category.Id,
                    Name = category.GetName(request.LanguageCode),
                    Description = category.GetDescription(request.LanguageCode),
                    IconUrl = category.IconUrl,
                    ColorHex = category.ColorHex,
                    DisplayOrder = category.DisplayOrder,
                    IsActive = category.IsActive,
                    IsLeaf = category.IsLeaf(),
                    ParentCategoryId = category.ParentCategoryId,
                    ParentCategoryName = category.ParentCategory?.GetName(request.LanguageCode),
                    SubCategoryCount = category.GetSubCategoryCount(),
                    ServiceCount = category.GetServiceCount(),
                    Depth = category.GetDepth()
                };

                return Result<ServiceCategoryDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<ServiceCategoryDto>.Failure(
                    Error.Internal("Kategori getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}