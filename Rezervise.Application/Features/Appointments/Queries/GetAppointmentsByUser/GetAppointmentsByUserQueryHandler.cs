using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace Rezervise.Application.Features.Appointments.Queries.GetAppointmentsByUser
{
    public class GetAppointmentsByUserQueryHandler : IRequestHandler<GetAppointmentsByUserQuery, Result<List<AppointmentDto>>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly ICurrentUserService _currentUser;

        public GetAppointmentsByUserQueryHandler(
            IRepository<Appointment> appointmentRepository,
            ICurrentUserService currentUser)
        {
            _appointmentRepository = appointmentRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<List<AppointmentDto>>> Handle(GetAppointmentsByUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<List<AppointmentDto>>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                if (!CallerAuthorization.CanActAsUser(_currentUser, request.UserId))
                    return Result<List<AppointmentDto>>.Failure(Error.Forbidden("Bu kullaniciya ait randevulari goruntuleme yetkiniz yok."));

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

                // 2. Verileri getir.
                //    Include edilmezse DTO'daki tum navigation alanlari (personel, isletme,
                //    hizmet, kullanici) null doner ve duzenleme ekrani bilgi gosteremez.
                var appointments = await _appointmentRepository.FindWithIncludesAsync(
                    predicate,
                    new[]
                    {
                        nameof(Appointment.Employee),   // EmployeeFullName, EmployeeTitle
                        nameof(Appointment.User),       // UserFullName, UserEmail
                        nameof(Appointment.Business),   // BusinessName
                        nameof(Appointment.Service)     // ServiceName
                    },
                    cancellationToken);

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
            catch (Exception)
            {
                return Result<List<AppointmentDto>>.Failure(
                    Error.Internal("Randevular getirilirken bir hata olustu."));
            }
        }
    }
}
