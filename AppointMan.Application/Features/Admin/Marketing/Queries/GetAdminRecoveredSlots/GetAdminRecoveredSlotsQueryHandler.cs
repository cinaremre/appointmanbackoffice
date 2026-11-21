using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminRecoveredSlots
{
    /// <summary>
    /// Kurtarilan kapasite raporunu uretir: slotlar acilis zamanina gore tersten
    /// siralanir; toplam Rebooked sayisi ozet alanda raporlanir.
    /// </summary>
    public class GetAdminRecoveredSlotsQueryHandler
        : IRequestHandler<GetAdminRecoveredSlotsQuery, Result<AdminRecoveredSlotReportDto>>
    {
        private readonly IRepository<RecoveredSlot> _recoveredSlotRepository;

        public GetAdminRecoveredSlotsQueryHandler(IRepository<RecoveredSlot> recoveredSlotRepository)
        {
            _recoveredSlotRepository = recoveredSlotRepository;
        }

        public async Task<Result<AdminRecoveredSlotReportDto>> Handle(
            GetAdminRecoveredSlotsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var businessId = request.BusinessId;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                var slots = await _recoveredSlotRepository.FindAsync(
                    r => !r.IsDeleted
                        && (businessId == null || r.BusinessId == businessId)
                        && (fromDate == null || r.ReleasedAt >= fromDate)
                        && (toDate == null || r.ReleasedAt <= toDate),
                    cancellationToken);

                var ordered = slots
                    .OrderByDescending(r => r.ReleasedAt)
                    .ToList();

                return Result<AdminRecoveredSlotReportDto>.Success(new AdminRecoveredSlotReportDto
                {
                    Items = ordered
                        .Select(r => new AdminRecoveredSlotDto
                        {
                            Id = r.Id,
                            BusinessId = r.BusinessId,
                            CancelledAppointmentId = r.CancelledAppointmentId,
                            ReplacementAppointmentId = r.ReplacementAppointmentId,
                            ReleasedAt = r.ReleasedAt,
                            RebookedAt = r.RebookedAt,
                            Status = r.Status
                        })
                        .ToList(),
                    TotalRebooked = ordered.Count(r => r.Status == EnumRecoveredSlotStatus.Rebooked)
                });
            }
            catch (Exception ex)
            {
                return Result<AdminRecoveredSlotReportDto>.Failure(
                    Error.Internal("Kurtarilan slot raporu getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
