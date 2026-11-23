using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Services.Queries.GetAdminServices
{
    /// <summary>
    /// Platform yöneticisi hizmet kataloğunu sayfalı listeler
    /// (doküman Modül 10). Sayfa boyutu 1..200 arasına sınırlanır,
    /// varsayılan 50'dir.
    /// </summary>
    public class GetAdminServicesQuery : IRequest<Result<AdminServiceListResultDto>>
    {
        public Guid? BusinessId { get; set; }
        public Guid? ServiceCategoryId { get; set; }
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AdminServiceListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>Sayfalı liste sonucu; controller pagination meta'sını buradan üretir.</summary>
    public class AdminServiceListResultDto
    {
        public IReadOnlyList<AdminServiceListItemDto> Items { get; set; } = Array.Empty<AdminServiceListItemDto>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
