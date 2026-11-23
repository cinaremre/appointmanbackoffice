using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Queries.GetAppointmentById
{
    public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, Result<AppointmentDto>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly ICurrentUserService _currentUser;

        public GetAppointmentByIdQueryHandler(
            IRepository<Appointment> appointmentRepository,
            ICurrentUserService currentUser)
        {
            _appointmentRepository = appointmentRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AppointmentDto>> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<AppointmentDto>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                // 1. Randevuyu ID'ye göre getir.
                //    Include edilmezse personel, işletme, hizmet ve kullanıcı alanları null döner.
                //    Silinmiş kayıtlar da 404 döndürür.
                var matches = await _appointmentRepository.FindWithIncludesAsync(
                    a => a.Id == request.AppointmentId && !a.IsDeleted,
                    new[]
                    {
                        nameof(Appointment.Employee),   // EmployeeFullName, EmployeeTitle
                        nameof(Appointment.User),       // UserFullName, UserEmail
                        nameof(Appointment.Business),   // BusinessName
                        nameof(Appointment.Service)     // ServiceName
                    },
                    cancellationToken);

                var appointment = matches.FirstOrDefault();

                // 2. Randevu bulunamadıysa hata döndür
                if (appointment is null)
                    return Result<AppointmentDto>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // 3. Yetki: randevu sahibi, işletme sahibi veya yönetici.
                var canRead =
                    CallerAuthorization.CanActAsUser(_currentUser, appointment.UserId) ||
                    (appointment.Business is not null &&
                     CallerAuthorization.CanActForBusiness(_currentUser, appointment.Business.OwnerId));

                if (!canRead)
                    return Result<AppointmentDto>.Failure(Error.Forbidden("Bu randevuyu görüntüleme yetkiniz yok."));

                // 4. Verileri DTO'ya dönüştür
                var dto = new AppointmentDto
                {
                    Id = appointment.Id,
                    UserId = appointment.UserId,
                    BusinessId = appointment.BusinessId,
                    ServiceId = appointment.ServiceId,
                    EmployeeId = appointment.EmployeeId,
                    AppointmentDate = appointment.AppointmentDate,
                    EndTime = appointment.EndTime,
                    Status = appointment.Status.ToString(),
                    Notes = appointment.Notes,
                    CancellationReason = appointment.CancellationReason,
                    CancelledAt = appointment.CancelledAt,
                    CompletedAt = appointment.CompletedAt,
                    IsReminderSent = appointment.IsReminderSent,
                    ReminderSentAt = appointment.ReminderSentAt,
                    CreatedAt = appointment.CreatedAt,

                    // Navigation property bilgileri
                    UserFullName = appointment.User?.GetFullName(),
                    UserEmail = appointment.User?.Email,
                    BusinessName = appointment.Business?.Name,
                    ServiceName = appointment.Service?.Name,
                    EmployeeFullName = appointment.Employee?.GetFullName(),
                    EmployeeTitle = appointment.Employee?.Title
                };

                return Result<AppointmentDto>.Success(dto);
            }
            catch (Exception)
            {
                return Result<AppointmentDto>.Failure(Error.Internal("Randevu bilgileri getirilirken bir hata oluştu."));
            }
        }
    }
}
