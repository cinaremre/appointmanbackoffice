using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminRecoveredSlots
{
    /// <summary>
    /// Kurtarilan kapasite raporu (dokuman Modul 15): iptallerden aciga cikan
    /// slotlar ve son durumlari. Filtreler ReleaseAt uzerinden calisir.
    /// </summary>
    public class GetAdminRecoveredSlotsQuery : IRequest<Result<AdminRecoveredSlotReportDto>>
    {
        /// <summary>Yalnizca bu isletmenin slotlari.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Slot acilis zamani alt siniri (ReleasedAt >= FromDate).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Slot acilis zamani ust siniri (ReleasedAt &lt;= ToDate).</summary>
        public DateTime? ToDate { get; set; }
    }
}
