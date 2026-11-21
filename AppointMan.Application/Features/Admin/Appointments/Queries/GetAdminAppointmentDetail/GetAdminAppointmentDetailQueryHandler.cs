using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentDetail
{
    /// <summary>
    /// Randevuyu ve kisa durum gecmisini getirir. Gecmis kayitlari
    /// degisiklik zamanina gore azalan sirada doner; ilk 20 kayit
    /// yeterlidir (dokuman Modul 12).
    /// </summary>
    public class GetAdminAppointmentDetailQueryHandler : IRequestHandler<GetAdminAppointmentDetailQuery, Result<AdminAppointmentDetailDto>>
    {
        /// <summary>Detayda dondurulen en fazla gecmis kayidi sayisi.</summary>
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
                // 1. Randevuyu yukle; yoksa NotFound.
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<AdminAppointmentDetailDto>.Failure(
                        Error.NotFoundById("Randevu", request.AppointmentId));

                // 2. Kisa gecmis: randevunun kayitlari, en yeni degisiklik basta.
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

                // 3. Detayi olustur.
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
                    Error.Internal("Randevu detayi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
