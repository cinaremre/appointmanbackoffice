using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Categories.Queries.GetBusinessCategoryUsage
{
    /// <summary>
    /// Kategoriye bagli aktif isletme sayisini dondurur (dokuman Modul 9).
    /// Arsiv oncesi etki analizi icin kullanilir.
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
