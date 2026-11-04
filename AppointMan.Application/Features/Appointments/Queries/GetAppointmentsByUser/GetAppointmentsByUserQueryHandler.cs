using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.Appointments.Queries.GetAppointmentsByUser
{
    public class GetAppointmentsByUserQueryHandler : IRequestHandler<GetAppointmentsByUserQuery, Result<List<AppointmentDto>>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;

        public GetAppointmentsByUserQueryHandler(IRepository<Appointment> appointmentRepository)
        {
            _appointmentRepository = appointmentRepository;
        }

        public async Task<Result<List<AppointmentDto>>> Handle(GetAppointmentsByUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Filtreleme
                Expression<Func<Appointment, bool>> predicate = a => a.UserId == request.UserId && !a.IsDeleted;

                if (request.StartDate.HasValue)
                {
                    var start = request.StartDate.Value.Date;
                    predicate = predicate.AndAlso(a => a.AppointmentDate >= start);
                }

                if (request.EndDate.HasValue)
                {
                    var end = request.EndDate.Value.Date.AddDays(1);
                    predicate = predicate.AndAlso(a => a.AppointmentDate < end);
                }

                if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<EnumAppointmentStatus>(request.Status, true, out var status))
                {
                    predicate = predicate.AndAlso(a => a.Status == status);
                }

                // 2. Verileri getir
                var appointments = await _appointmentRepository.FindAsync(predicate, cancellationToken);

                // 3. Sayfalama
                var pagedAppointments = appointments
                    .OrderBy(a => a.AppointmentDate)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 4. DTO'ya donustur
                var dtos = pagedAppointments.Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    BusinessId = a.BusinessId,
                    ServiceId = a.ServiceId,
                    EmployeeId = a.EmployeeId,
                    AppointmentDate = a.AppointmentDate,
                    EndTime = a.EndTime,
                    Status = a.Status.ToString(),
                    Notes = a.Notes,
                    CancellationReason = a.CancellationReason,
                    CancelledAt = a.CancelledAt,
                    CompletedAt = a.CompletedAt,
                    IsReminderSent = a.IsReminderSent,
                    ReminderSentAt = a.ReminderSentAt,
                    CreatedAt = a.CreatedAt,
                    UserFullName = a.User?.GetFullName(),
                    UserEmail = a.User?.Email,
                    BusinessName = a.Business?.Name,
                    ServiceName = a.Service?.Name,
                    EmployeeFullName = a.Employee?.GetFullName(),
                    EmployeeTitle = a.Employee?.Title
                }).ToList();

                return Result<List<AppointmentDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<AppointmentDto>>.Failure(
                    Error.Internal("Randevular getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}