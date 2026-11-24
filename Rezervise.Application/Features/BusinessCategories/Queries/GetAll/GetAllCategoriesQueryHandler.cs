using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.BusinessCategories.Queries.GetAll
{
    /// <summary>
    /// İşletme kategorilerini ana/alt kategori hiyerarşisi olarak döndürür.
    /// Sayaçlar ilişkiler üzerinden hesaplanır; böylece subCategoryCount ve
    /// businessCount her zaman gerçek değerleri yansıtır.
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
                // Sayaçlar ve parent adı için ilişkiler tek sorguda yüklenir;
                // aksi halde navigation koleksiyonları boş gelir ve sayaçlar 0 döner.
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

                // Alt kategoriler ana kategorilerin altına yerleştirilir.
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
                    // Kök kategorilerde CorrespondingBusinessType doldurulur;
                    // mobil hizmet ekleme akışında "Diğer" (99) varsayılanı kullanılır.
                    root.CorrespondingBusinessType ??= EnumBusinessType.Other;

                    // Filtre sonrası gerçek alt kategori sayısı ve sırası.
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
                    Error.Internal("Kategoriler getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
