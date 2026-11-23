using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminRecoveredSlots
{
    /// <summary>
    /// Kurtarılan kapasite raporu (doküman Modül 15): iptallerden açığa çıkan
    /// slotlar ve son durumları. Filtreler ReleaseAt üzerinden çalışır.
    /// </summary>
    public class GetAdminRecoveredSlotsQuery : IRequest<Result<AdminRecoveredSlotReportDto>>
    {
        /// <summary>Yalnızca bu işletmenin slotları.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Slot açılış zamanı alt sınırı (ReleasedAt >= FromDate).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Slot açılış zamanı üst sınırı (ReleasedAt &lt;= ToDate).</summary>
        public DateTime? ToDate { get; set; }
    }
}
