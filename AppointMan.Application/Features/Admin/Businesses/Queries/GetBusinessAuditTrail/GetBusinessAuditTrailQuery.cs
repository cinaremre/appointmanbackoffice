using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Businesses.Queries.GetBusinessAuditTrail
{
    /// <summary>
    /// Isletme uzerinde yapilan yonetici islemlerinin denetim kaydi
    /// (dokuman Modul 21). Kayitlar CreatedAtUtc'ye gore tersten siralanir.
    /// </summary>
    public class GetBusinessAuditTrailQuery : IRequest<Result<AdminAuditTrailResult>>
    {
        public Guid BusinessId { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 50;
    }

    /// <summary>Sayfalanmis denetim kaydi sonucu.</summary>
    public class AdminAuditTrailResult
    {
        public IReadOnlyList<AdminAuditTrailItemDto> Items { get; init; } = Array.Empty<AdminAuditTrailItemDto>();

        public int Page { get; init; }

        public int PageSize { get; init; }

        public long TotalCount { get; init; }
    }

    /// <summary>Tek bir denetim kaydi satiri.</summary>
    public class AdminAuditTrailItemDto
    {
        public Guid ActorUserId { get; init; }

        public string ActorRole { get; init; } = string.Empty;

        public string Action { get; init; } = string.Empty;

        public string? Reason { get; init; }

        public DateTime CreatedAtUtc { get; init; }
    }
}
