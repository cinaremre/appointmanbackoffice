using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.AppointmentHistories.Queries.GetByBusiness
{
    public class GetAppointmentHistoryByBusinessQueryHandler : IRequestHandler<GetAppointmentHistoryByBusinessQuery, Result<List<AppointmentHistoryDto>>>
    {
        private readonly IRepository<AppointmentHistory> _historyRepository;
        private readonly IRepository<Appointment> _appointmentRepository;

        public GetAppointmentHistoryByBusinessQueryHandler(
            IRepository<AppointmentHistory> historyRepository,
            IRepository<Appointment> appointmentRepository)
        {
            _historyRepository = historyRepository;
            _appointmentRepository = appointmentRepository;
        }

        public async Task<Result<List<AppointmentHistoryDto>>> Handle(GetAppointmentHistoryByBusinessQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Once isletmeye ait randevu ID'lerini bul
                var appointments = await _appointmentRepository.FindAsync(
                    a => a.BusinessId == request.BusinessId && !a.IsDeleted,
                    cancellationToken);

                var appointmentIds = appointments.Select(a => a.Id).ToList();

                if (!appointmentIds.Any())
                    return Result<List<AppointmentHistoryDto>>.Success(new List<AppointmentHistoryDto>());

                // 2. Bu randevulara ait gecmis kayitlarini getir
                Expression<Func<AppointmentHistory, bool>> predicate = h =>
                    appointmentIds.Contains(h.AppointmentId) && !h.IsDeleted;

                if (request.StartDate.HasValue)
                {
                    var start = request.StartDate.Value.Date;
                    predicate = predicate.AndAlso(h => h.ChangedAt >= start);
                }

                if (request.EndDate.HasValue)
                {
                    var end = request.EndDate.Value.Date.AddDays(1);
                    predicate = predicate.AndAlso(h => h.ChangedAt < end);
                }

                var histories = await _historyRepository.FindAsync(predicate, cancellationToken);

                // 3. Sayfalama
                var pagedHistories = histories
                    .OrderByDescending(h => h.ChangedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 4. DTO'ya donustur
                var dtos = pagedHistories.Select(h => new AppointmentHistoryDto
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