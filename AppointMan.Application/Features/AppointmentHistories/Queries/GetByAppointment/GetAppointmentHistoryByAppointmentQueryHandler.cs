using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.AppointmentHistories.Queries.GetByAppointment
{
    public class GetAppointmentHistoryByAppointmentQueryHandler : IRequestHandler<GetAppointmentHistoryByAppointmentQuery, Result<List<AppointmentHistoryDto>>>
    {
        private readonly IRepository<AppointmentHistory> _historyRepository;

        public GetAppointmentHistoryByAppointmentQueryHandler(IRepository<AppointmentHistory> historyRepository)
        {
            _historyRepository = historyRepository;
        }

        public async Task<Result<List<AppointmentHistoryDto>>> Handle(GetAppointmentHistoryByAppointmentQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Randevunun tum gecmisini getir
                var histories = await _historyRepository.FindAsync(
                    h => h.AppointmentId == request.AppointmentId && !h.IsDeleted,
                    cancellationToken);

                // 2. Zamana gore sirala (en yeni en basta)
                var sortedHistories = histories
                    .OrderByDescending(h => h.ChangedAt)
                    .ToList();

                // 3. DTO'ya donustur
                var dtos = sortedHistories.Select(h => new AppointmentHistoryDto
                {
                    Id = h.Id,
                    AppointmentId = h.AppointmentId,
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    ChangeReason = h.ChangeReason,
                    ChangedAt = h.ChangedAt,
                    ChangedBy = h.ChangedBy,
                    IpAddress = h.IpAddress,
                    UserAgent = h.UserAgent,
                    AdditionalData = h.AdditionalData,
                    OldStatusName = h.OldStatus.ToString(),
                    NewStatusName = h.NewStatus.ToString()
                }).ToList();

                return Result<List<AppointmentHistoryDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<AppointmentHistoryDto>>.Failure(
                    Error.Internal("Randevu gecmisi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}