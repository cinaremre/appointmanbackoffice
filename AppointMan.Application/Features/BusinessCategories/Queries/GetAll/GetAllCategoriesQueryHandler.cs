using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.BusinessCategories.Queries.GetAll
{
    /// <summary>
    /// Isletme kategorilerini ana/alt kategori hiyerarsisi olarak dondurur.
    /// Sayaclar iliskiler uzerinden hesaplanir; boylece subCategoryCount ve
    /// businessCount her zaman gercek degerleri yansitir.
    /// </summary>
    public class GetAllCategoriesQueryHandler
        : IRequestHandler<GetAllCategoriesQuery, Result<List<BusinessCategoryDto>>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;

        public GetAllCategoriesQueryHandler(IRepository<BusinessCategory> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<List<BusinessCategoryDto>>> Handle(
            GetAllCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Sayaclar ve parent adi icin iliskiler tek sorguda yuklenir;
                // aksi halde navigation koleksiyonlari bos gelir ve sayaclar 0 doner.
                var categories = await _categoryRepository.FindWithIncludesAsync(
                    c => !c.IsDeleted,
                    new[] { "ParentCategory", "SubCategories", "Businesses" },
                    cancellationToken);

                var onlyActive = request.OnlyActive == true;

                var dtosById = categories
                    .Where(c => !onlyActive || c.IsActive)
                    .ToDictionary(c => c.Id, c => new BusinessCategoryDto
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
                    });

                // Alt kategoriler ana kategorilerin altina yerlestirilir.
                foreach (var dto in dtosById.Values.Where(d => d.ParentCategoryId.HasValue).ToList())
                {
                    if (dtosById.TryGetValue(dto.ParentCategoryId!.Value, out var parent))
                        parent.SubCategories.Add(dto);
                }

                var roots = dtosById.Values
                    .Where(d => !d.ParentCategoryId.HasValue)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.Name)
                    .ToList();

                foreach (var root in roots)
                {
                    // Filtre sonrasi gercek alt kategori sayisi ve sirasi.
                    root.SubCategories = root.SubCategories
                        .OrderBy(s => s.DisplayOrder)
                        .ThenBy(s => s.Name)
                        .ToList();

                    root.SubCategoryCount = root.SubCategories.Count;
                }

                return Result<List<BusinessCategoryDto>>.Success(roots);
            }
            catch (Exception ex)
            {
                return Result<List<BusinessCategoryDto>>.Failure(
                    Error.Internal("Kategoriler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
