using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminReengagementAttempts
{
    /// <summary>
    /// Geri çağırma denemeleri listesi (doküman Modül 15). İşletme, durum ve
    /// tarih filtreleriyle ReengagementAttempt kayıtlarını getirir.
    /// </summary>
    public class GetAdminReengagementAttemptsQuery : IRequest<Result<AdminReengagementAttemptListDto>>
    {
        /// <summary>Yalnızca bu işletmenin denemeleri.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Deneme durumu filtresi (EnumReengagementStatus).</summary>
        public EnumReengagementStatus? Status { get; set; }

        /// <summary>Tetiklenme zamanı alt sınırı (TriggeredAt >= FromDate).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Tetiklenme zamanı üst sınırı (TriggeredAt &lt;= ToDate).</summary>
        public DateTime? ToDate { get; set; }
    }
}
