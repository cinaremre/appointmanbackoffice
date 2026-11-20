using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.Appointments.Queries.GetAppointmentsByBusiness
{
    public class GetAppointmentsByBusinessQueryHandler : IRequestHandler<GetAppointmentsByBusinessQuery, Result<List<AppointmentDto>>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly ICurrentUserService _currentUser;

        public GetAppointmentsByBusinessQueryHandler(
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser)
        {
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<List<AppointmentDto>>> Handle(GetAppointmentsByBusinessQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<List<AppointmentDto>>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                // Yetki, govdedeki id'ye degil yuklenen isletmenin sahibine gore verilir.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<List<AppointmentDto>>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<List<AppointmentDto>>.Failure(Error.Forbidden("Bu isletmenin randevularini goruntuleme yetkiniz yok."));

                // 1. Filtreleme
                Expression<Func<Appointment, bool>> predicate = a => a.BusinessId == request.BusinessId && !a.IsDeleted;

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
                    AppointmentDate = a.AppointmentDate,
                    EndTime = a.EndTime,
                    Status = a.Status.ToString(),
                    Notes = a.Notes,
                    CancellationReason = a.CancellationReason,
                    CancelledAt = a.CancelledAt,
                    CompletedAt = a.CompletedAt,
                    IsReminderSent = a.IsReminderSent,
                    ReminderSentAt = a.ReminderSentAt,
                    CreatedAt = a.CreatedAt
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
