using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.BusinessCategories.Queries.GetAll
{
    public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, Result<List<BusinessCategoryDto>>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;

        public GetAllCategoriesQueryHandler(IRepository<BusinessCategory> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<List<BusinessCategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kategorileri getir
                var categories = await _categoryRepository.FindAsync(
                    c => !c.IsDeleted,
                    cancellationToken);

                // 2. Aktif filtreleme
                var filteredCategories = categories.AsEnumerable();

                if (request.OnlyActive.HasValue && request.OnlyActive.Value)
                {
                    filteredCategories = filteredCategories.Where(c => c.IsActive);
                }

                // 3. Siralama
                var sortedCategories = filteredCategories
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToList();

                // 4. DTO'ya donustur
                var dtos = sortedCategories.Select(c => new BusinessCategoryDto
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
                    Error.Internal("Kategoriler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}