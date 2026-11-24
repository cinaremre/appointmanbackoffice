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

namespace Rezervise.Application.Features.Appointments.Queries.GetAppointmentsByBusiness
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

                // Yetki, gövdedeki id'ye değil yüklenen işletmenin sahibine göre verilir.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<List<AppointmentDto>>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<List<AppointmentDto>>.Failure(Error.Forbidden("Bu işletmenin randevularını görüntüleme yetkiniz yok."));

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

                // 2. Verileri getir.
                //    User/Service/Employee navigasyonları yüklenmezse DTO'daki ad alanları
                //    (userFullName, serviceName, employeeFullName) null döner; detayendpoint'i
                //    bunları doldurduğu için liste de aynı şekilde dolu gelmelidir.
                var appointments = await _appointmentRepository.FindWithIncludesAsync(
                    predicate,
                    new[]
                    {
                        nameof(Appointment.User),
                        nameof(Appointment.Service),
                        nameof(Appointment.Employee)
                    },
                    cancellationToken);

                // 3. Sayfalama
                var pagedAppointments = appointments
                    .OrderBy(a => a.AppointmentDate)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 4. DTO'ya dönüştür
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
                    CreatedAt = a.CreatedAt,

                    // Ad alanları: işletme randevu listesinde dolu gelmeli.
                    UserFullName = a.User?.GetFullName(),
                    UserEmail = a.User?.Email,
                    ServiceName = a.Service?.Name,
                    EmployeeFullName = a.Employee?.GetFullName(),
                    EmployeeTitle = a.Employee?.Title
                }).ToList();

                return Result<List<AppointmentDto>>.Success(dtos);
            }
            catch (Exception)
            {
                return Result<List<AppointmentDto>>.Failure(
                    Error.Internal("Randevular getirilirken bir hata oluştu."));
            }
        }
    }
}
