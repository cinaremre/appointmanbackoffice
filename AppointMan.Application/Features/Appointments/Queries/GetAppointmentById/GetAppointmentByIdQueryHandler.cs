using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Queries.GetAppointmentById
{
    public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, Result<AppointmentDto>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;

        public GetAppointmentByIdQueryHandler(IRepository<Appointment> appointmentRepository)
        {
            _appointmentRepository = appointmentRepository;
        }

        public async Task<Result<AppointmentDto>> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Randevuyu ID'ye gore getir
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);

                // 2. Randevu bulunamadiysa hata dondur
                if (appointment is null)
                    return Result<AppointmentDto>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // 3. Verileri DTO'ya donustur
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
            catch (Exception ex)
            {
                return Result<AppointmentDto>.Failure(Error.Internal("Randevu bilgileri getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}