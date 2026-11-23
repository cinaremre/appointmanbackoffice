using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentDetail
{
    /// <summary>
    /// Randevuyu ve kısa durum geçmişini getirir. Geçmiş kayıtları
    /// değişiklik zamanına göre azalan sırada döner; ilk 20 kayıt
    /// yeterlidir (doküman Modül 12).
    /// </summary>
    public class GetAdminAppointmentDetailQueryHandler : IRequestHandler<GetAdminAppointmentDetailQuery, Result<AdminAppointmentDetailDto>>
    {
        /// <summary>Detayda döndürülen en fazla geçmiş kayıdı sayısı.</summary>
        public const int HistoryLimit = 20;

        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<AppointmentHistory> _historyRepository;

        public GetAdminAppointmentDetailQueryHandler(
            IRepository<Appointment> appointmentRepository,
            IRepository<AppointmentHistory> historyRepository)
        {
            _appointmentRepository = appointmentRepository;
            _historyRepository = historyRepository;
        }

        public async Task<Result<AdminAppointmentDetailDto>> Handle(
            GetAdminAppointmentDetailQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Randevuyu yükle; yoksa NotFound.
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<AdminAppointmentDetailDto>.Failure(
                        Error.NotFoundById("Randevu", request.AppointmentId));

                // 2. Kısa geçmiş: randevunun kayıtları, en yeni değişiklik başta.
                var history = await _historyRepository.FindAsync(
                    h => h.AppointmentId == request.AppointmentId,
                    cancellationToken);

                var historyItems = history
                    .OrderByDescending(h => h.ChangedAt)
                    .ThenByDescending(h => h.Id)
                    .Take(HistoryLimit)
                    .Select(h => new AdminAppointmentHistoryDto
                    {
                        Id = h.Id,
                        OldStatus = h.OldStatus,
                        NewStatus = h.NewStatus,
                        ChangeReason = h.ChangeReason,
                        ChangedBy = h.ChangedBy,
                        ChangedAt = h.ChangedAt
                    })
                    .ToList();

                // 3. Detayı oluştur.
                var dto = new AdminAppointmentDetailDto
                {
                    Id = appointment.Id,
                    BusinessId = appointment.BusinessId,
                    UserId = appointment.UserId,
                    EmployeeId = appointment.EmployeeId,
                    ServiceId = appointment.ServiceId,
                    AppointmentDate = appointment.AppointmentDate,
                    Status = appointment.Status,
                    Notes = appointment.Notes,
                    CancellationReason = appointment.CancellationReason,
                    CancelledAt = appointment.CancelledAt,
                    CreatedAt = appointment.CreatedAt,
                    History = historyItems
                };

                return Result<AdminAppointmentDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<AdminAppointmentDetailDto>.Failure(
                    Error.Internal("Randevu detayı getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
