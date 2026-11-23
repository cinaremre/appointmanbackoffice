using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Queries.GetBusinessCategoryUsage
{
    /// <summary>
    /// Kategoriye bağlı aktif işletme sayısını döndürür (doküman Modül 9).
    /// Arşiv öncesi etki analizi için kullanılır.
    /// </summary>
    public class GetBusinessCategoryUsageQuery : IRequest<Result<BusinessCategoryUsageDto>>
    {
        public Guid CategoryId { get; set; }
    }

    public class BusinessCategoryUsageDto
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BusinessCount { get; set; }
    }
}
