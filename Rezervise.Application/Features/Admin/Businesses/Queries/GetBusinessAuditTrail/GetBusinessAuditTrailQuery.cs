using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinessAuditTrail
{
    /// <summary>
    /// İşletme üzerinde yapılan yönetici işlemlerinin denetim kaydı
    /// (doküman Modül 21). Kayıtlar CreatedAtUtc'ye göre tersten sıralanır.
    /// </summary>
    public class GetBusinessAuditTrailQuery : IRequest<Result<AdminAuditTrailResult>>
    {
        public Guid BusinessId { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 50;
    }

    /// <summary>Sayfalanmış denetim kaydı sonucu.</summary>
    public class AdminAuditTrailResult
    {
        public IReadOnlyList<AdminAuditTrailItemDto> Items { get; init; } = Array.Empty<AdminAuditTrailItemDto>();

        public int Page { get; init; }

        public int PageSize { get; init; }

        public long TotalCount { get; init; }
    }

    /// <summary>Tek bir denetim kaydı satırı.</summary>
    public class AdminAuditTrailItemDto
    {
        public Guid ActorUserId { get; init; }

        public string ActorRole { get; init; } = string.Empty;

        public string Action { get; init; } = string.Empty;

        public string? Reason { get; init; }

        public DateTime CreatedAtUtc { get; init; }
    }
}
