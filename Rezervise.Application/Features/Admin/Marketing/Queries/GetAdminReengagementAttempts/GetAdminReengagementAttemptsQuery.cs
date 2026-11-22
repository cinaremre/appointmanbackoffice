using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminReengagementAttempts
{
    /// <summary>
    /// Geri cagirma denemeleri listesi (dokuman Modul 15). Isletme, durum ve
    /// tarih filtreleriyle ReengagementAttempt kayitlarini getirir.
    /// </summary>
    public class GetAdminReengagementAttemptsQuery : IRequest<Result<AdminReengagementAttemptListDto>>
    {
        /// <summary>Yalnizca bu isletmenin denemeleri.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Deneme durumu filtresi (EnumReengagementStatus).</summary>
        public EnumReengagementStatus? Status { get; set; }

        /// <summary>Tetiklenme zamani alt siniri (TriggeredAt >= FromDate).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Tetiklenme zamani ust siniri (TriggeredAt &lt;= ToDate).</summary>
        public DateTime? ToDate { get; set; }
    }
}
