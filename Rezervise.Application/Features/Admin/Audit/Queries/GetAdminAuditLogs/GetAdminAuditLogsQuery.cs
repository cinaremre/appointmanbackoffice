using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Audit.Queries.GetAdminAuditLogs
{
    /// <summary>
    /// Yönetici denetim kayıtları listesi (doküman Modül 21). Append-only
    /// kayıtlar CreatedAtUtc tersten sıralanarak dondurulur.
    /// </summary>
    public class GetAdminAuditLogsQuery : IRequest<Result<AdminAuditLogListDto>>
    {
        /// <summary>Yalnızca bu yöneticinin işlemleri.</summary>
        public Guid? ActorUserId { get; set; }

        /// <summary>Yalnızca bu işletmeyle ilgili işlemler.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>İşlem yapılan varlık tipi ("Business", "Notification" vb.).</summary>
        public string? EntityType { get; set; }

        /// <summary>Kayıt zamanı alt sınırı (CreatedAtUtc >= FromDate).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Kayıt zamanı üst sınırı (CreatedAtUtc &lt;= ToDate).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanlı sayfa numarası; kırpanma kuralı AdminPagination'dadır.</summary>
        public int Page { get; set; }

        /// <summary>Sayfa boyutu; en fazla 200, varsayılan 50.</summary>
        public int PageSize { get; set; }
    }
}
