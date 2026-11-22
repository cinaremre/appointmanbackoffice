using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Audit.Queries.GetAdminAuditLogs
{
    /// <summary>
    /// Yonetici denetim kayitlari listesi (dokuman Modul 21). Append-only
    /// kayitlar CreatedAtUtc tersten siralanarak dondurulur.
    /// </summary>
    public class GetAdminAuditLogsQuery : IRequest<Result<AdminAuditLogListDto>>
    {
        /// <summary>Yalnizca bu yoneticinin islemleri.</summary>
        public Guid? ActorUserId { get; set; }

        /// <summary>Yalnizca bu isletmeyle ilgili islemler.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Islem yapilan varlik tipi ("Business", "Notification" vb.).</summary>
        public string? EntityType { get; set; }

        /// <summary>Kayit zamani alt siniri (CreatedAtUtc >= FromDate).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Kayit zamani ust siniri (CreatedAtUtc &lt;= ToDate).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanli sayfa numarasi; kirpanma kurali AdminPagination'dadir.</summary>
        public int Page { get; set; }

        /// <summary>Sayfa boyutu; en fazla 200, varsayilan 50.</summary>
        public int PageSize { get; set; }
    }
}
