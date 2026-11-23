using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Queries.GetBusinessCategoryTree
{
    /// <summary>
    /// Tüm işletme kategorilerini düz liste olarak döndürür.
    /// ChildCount ParentId sayımıyla hesaplanır; navigation yüklemesine
    /// ihtiyaç duyulmaz. Sıra: önce DisplayOrder, sonra ad.
    /// </summary>
    public class GetBusinessCategoryTreeQueryHandler
        : IRequestHandler<GetBusinessCategoryTreeQuery, Result<List<AdminBusinessCategoryTreeNodeDto>>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;

        public GetBusinessCategoryTreeQueryHandler(IRepository<BusinessCategory> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<List<AdminBusinessCategoryTreeNodeDto>>> Handle(
            GetBusinessCategoryTreeQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var categories = await _categoryRepository.FindAsync(
                    c => !c.IsDeleted,
                    cancellationToken);

                var visible = categories
                    .Where(c => request.IncludeInactive || c.IsActive)
                    .ToList();

                // Çocuk sayımı ParentId üzerinden: navigation koleksiyonları
                // fake/EF farklarından bağımsız, her zaman doğru sonuç verir.
                var childCountById = visible
                    .Where(c => c.ParentCategoryId.HasValue)
                    .GroupBy(c => c.ParentCategoryId!.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

                var nodes = visible
                    .Select(c => new AdminBusinessCategoryTreeNodeDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        ParentId = c.ParentCategoryId,
                        DisplayOrder = c.DisplayOrder,
                        IsActive = c.IsActive,
                        ChildCount = childCountById.TryGetValue(c.Id, out var count) ? count : 0
                    })
                    .OrderBy(n => n.DisplayOrder)
                    .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return Result<List<AdminBusinessCategoryTreeNodeDto>>.Success(nodes);
            }
            catch (Exception ex)
            {
                return Result<List<AdminBusinessCategoryTreeNodeDto>>.Failure(
                    Error.Internal("Kategori ağacı getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
