using System.Data;
using Rezervise.Application.Common.Abstractions;
using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Commands.RescheduleAppointment
{
    public class RescheduleAppointmentCommandHandler : IRequestHandler<RescheduleAppointmentCommand, Result<Guid>>
    {
        private const string BlacklistMessage = "Randevunuz oluşturulamıyor, lütfen işletme ile iletişime geçiniz.";

        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<BlacklistEntry> _blacklistRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeService _dateTimeService;
        private readonly IAppointmentBookingGuard _bookingGuard;
        private readonly RezerviseMetrics _metrics;
        private readonly ICurrentUserService _currentUser;

        public RescheduleAppointmentCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            IRepository<Employee> employeeRepository,
            IRepository<BlacklistEntry> blacklistRepository,
            IUnitOfWork unitOfWork,
            IDateTimeService dateTimeService,
            IAppointmentBookingGuard bookingGuard,
            RezerviseMetrics metrics,
            ICurrentUserService currentUser)
        {
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _employeeRepository = employeeRepository;
            _blacklistRepository = blacklistRepository;
            _unitOfWork = unitOfWork;
            _dateTimeService = dateTimeService;
            _bookingGuard = bookingGuard;
            _metrics = metrics;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var now = _dateTimeService.UtcNow;

                // Bütün kontroller ve mutation aynı SERIALIZABLE transaction içinde.
                var result = await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    // 1. Randevu kontrolü
                    var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, token);
                    if (appointment is null)
                        return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                    // 2. Yetki kontrolü, işletme ve çalışan bilgisi yüklendikten sonra
                    //    (4b) numaralı adımda yapılır; böylece işletme sahibi ve randevunun
                    //    çalışanı da randevuyu yeniden planlayabilir.

                    // 3. İşletme kontrolü
                    var business = await _businessRepository.GetByIdWithIncludesAsync(
                        appointment.BusinessId,
                        token,
                        b => b.WorkingHours);

                    if (business is null)
                        return Result<Guid>.Failure(Error.NotFound("İşletme", appointment.BusinessId.ToString()));

                    if (!business.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("İşletme aktif değil."));

                    // 4. Çalışan kontrolü (değişebilir veya aynı kalır)
                    var employeeId = request.NewEmployeeId ?? appointment.EmployeeId;

                    var employee = await _employeeRepository.GetByIdWithIncludesAsync(
                        employeeId,
                        token,
                        e => e.EmployeeServices,
                        e => e.WorkingHours,
                        e => e.TimeOffs);

                    if (employee is null)
                        return Result<Guid>.Failure(Error.NotFound("Çalışan", employeeId.ToString()));

                    if (!employee.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan aktif değil."));

                    if (!employee.IsAvailableForBooking)
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan su anda randevu kabul etmiyor."));

                    if (employee.BusinessId != appointment.BusinessId)
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan bu işletmeye ait değil."));

                    // 4b. Yetki kontrolü - randevu sahibi müşteri, işletme sahibi veya atanmış çalışan.
                    var isBusinessSide =
                        CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId) ||
                        employee.IsLinkedToUser(_currentUser.UserId.Value);

                    if (!isBusinessSide && !CallerAuthorization.CanActAsUser(_currentUser, appointment.UserId))
                        return Result<Guid>.Failure(Error.Forbidden("Bu randevuyu yeniden planlama yetkiniz yok."));

                    // 4c. Müşteri son 2 saatte değişiklik yapamaz; işletme ve çalışan muaf.
                    if (!isBusinessSide && appointment.IsWithinCustomerCutoff(now))
                        return Result<Guid>.Failure(Error.BusinessRule(
                            $"Randevuya {Appointment.CustomerChangeCutoffHours} saatten az kaldığı için değiştirilemez."));

                    // 5. Hizmet kontrolü
                    var service = await _serviceRepository.GetByIdAsync(appointment.ServiceId, token);
                    if (service is null)
                        return Result<Guid>.Failure(Error.NotFound("Hizmet", appointment.ServiceId.ToString()));

                    if (!service.IsActive)
                        return Result<Guid>.Failure(Error.BusinessRule("Hizmet aktif değil."));

                    // 6. Çalışan bu hizmeti verebiliyor mü?
                    if (!employee.CanProvideService(appointment.ServiceId))
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan bu hizmeti veremiyor."));

                    // 7. Çalışma saati kontrolü - UTC anını işletme yerel zamanına çevir.
                    var start = request.NewAppointmentDate;
                    var durationMinutes = service.DurationInMinutes;
                    var end = start.AddMinutes(durationMinutes);
                    var localStart = _dateTimeService.ToBusinessLocalTime(start);
                    var localEnd = localStart.AddMinutes(durationMinutes);

                    if (!business.IsOpenDuring(localStart, localEnd))
                        return Result<Guid>.Failure(Error.BusinessRule("İşletme bu saatte çalışmıyor."));

                    if (!employee.IsWorkingDuring(localStart, localEnd))
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan bu saatte çalışmıyor."));

                    if (employee.IsOnApprovedTimeOff(localStart, localEnd))
                        return Result<Guid>.Failure(Error.BusinessRule("Çalışan bu tarihte izinde."));

                    // 8. Çakışma kontrolü. Domain overlap formülü:
                    //    appointment.EndTime ?? appointment.AppointmentDate.AddMinutes(service.DurationInMinutes)
                    //    DB tarafında a.Service.DurationInMinutes kullanılmadan fallback süre ile uygulanır.
                    var hasConflict = await _bookingGuard.HasConflictAsync(
                        employeeId,
                        start,
                        end,
                        durationMinutes,
                        excludeAppointmentId: appointment.Id,
                        cancellationToken: token);

                    if (hasConflict)
                        return Result<Guid>.Failure(Error.Conflict("Çalışanın bu saatte başka bir randevusu var."));

                    // 9. Kara liste tekrar kontrolü (bu işlemde yeni kayıt oluşturulmaz).
                    if (await IsBlacklistedAsync(request.UserId, appointment.BusinessId, token))
                        return Result<Guid>.Failure(Error.BusinessRule(BlacklistMessage));

                    // 10. Aktif randevu limiti, yeniden planlanan randevu hariç.
                    var activeAppointmentCount = await CountActiveAppointmentsAsync(
                        request.UserId, appointment.BusinessId, appointment.Id, now, token);

                    if (activeAppointmentCount >= 2)
                        return Result<Guid>.Failure(
                            Error.BusinessRule("Aktif randevu limiti nedeniyle yeniden planlanamaz."));

                    // 11. Çalışan değişikliği kalıcı uygulanır; aynı çalışansa domain no-op yapar.
                    var changedBy = request.RescheduledBy ?? "User";
                    if (request.NewEmployeeId.HasValue)
                        appointment.ReassignEmployee(request.NewEmployeeId.Value, changedBy, now);

                    // 12. Randevuyu yeni tarihe taşır ve bitiş saatini servis süresinden yazar.
                    appointment.Reschedule(start, changedBy, now);
                    appointment.SetEndTime(end);

                    return Result<Guid>.Success(appointment.Id);
                }, IsolationLevel.Serializable, cancellationToken);

                if (result.IsSuccess)
                    _metrics.RecordAppointmentRescheduled();

                return result;
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(Error.Internal("Randevu yeniden planlanırken bir hata oluştu."));
            }
        }

        private async Task<bool> IsBlacklistedAsync(
            Guid userId,
            Guid businessId,
            CancellationToken cancellationToken)
        {
            return await _blacklistRepository.ExistsAsync(
                b => b.UserId == userId &&
                     b.BusinessId == businessId &&
                     !b.IsDeleted,
                cancellationToken);
        }

        private async Task<int> CountActiveAppointmentsAsync(
            Guid userId,
            Guid businessId,
            Guid excludeAppointmentId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var appointments = await _appointmentRepository.FindAsync(
                a => a.UserId == userId &&
                     a.BusinessId == businessId &&
                     a.Id != excludeAppointmentId &&
                     a.Status != EnumAppointmentStatus.Cancelled &&
                     a.Status != EnumAppointmentStatus.Completed &&
                     a.AppointmentDate > now &&
                     !a.IsDeleted,
                cancellationToken);

            return appointments.Count;
        }
    }
}
