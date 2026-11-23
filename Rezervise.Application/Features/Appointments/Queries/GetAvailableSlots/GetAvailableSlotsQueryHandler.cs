using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Queries.GetAvailableSlots
{
    public class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, Result<List<AvailableSlotDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IDateTimeService _dateTimeService;

        public GetAvailableSlotsQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Employee> employeeRepository,
            IRepository<Service> serviceRepository,
            IRepository<Appointment> appointmentRepository,
            IDateTimeService dateTimeService)
        {
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
            _serviceRepository = serviceRepository;
            _appointmentRepository = appointmentRepository;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<List<AvailableSlotDto>>> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü.
                //    WorkingHours yüklenmezse IsOpenOn() yanlış sonuç verir.
                var businesses = await _businessRepository.FindWithIncludesAsync(
                    b => b.Id == request.BusinessId && !b.IsDeleted,
                    new[] { nameof(Business.WorkingHours) },
                    cancellationToken);

                var business = businesses.FirstOrDefault();
                if (business is null)
                    return Result<List<AvailableSlotDto>>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                if (!business.IsActive)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("İşletme aktif değil."));

                // 2. Çalışan kontrolü.
                //    Navigasyonlar yüklenmezse domain kontrolleri sessizce yanlış sonuç verir:
                //    CanProvideService() her zaman false döner (400) ve çalışma saati bulunamaz (boş liste).
                var employees = await _employeeRepository.FindWithIncludesAsync(
                    e => e.Id == request.EmployeeId && !e.IsDeleted,
                    new[]
                    {
                        nameof(Employee.EmployeeServices),  // CanProvideService
                        nameof(Employee.WorkingHours),      // GetWorkingHourByDay
                        nameof(Employee.TimeOffs)           // IsOnTimeOff
                    },
                    cancellationToken);

                var employee = employees.FirstOrDefault();
                if (employee is null)
                    return Result<List<AvailableSlotDto>>.Failure(Error.NotFound("Çalışan", request.EmployeeId.ToString()));

                if (!employee.IsActive)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Çalışan aktif değil."));

                if (employee.BusinessId != request.BusinessId)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Çalışan bu işletmeye ait değil."));

                // 3. Hizmet kontrolü
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null)
                    return Result<List<AvailableSlotDto>>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                if (!service.IsActive)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Hizmet aktif değil."));

                if (service.BusinessId != request.BusinessId)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Hizmet bu işletmeye ait değil."));

                // 4. Çalışan bu hizmeti verebiliyor mü?
                if (!employee.CanProvideService(request.ServiceId))
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Çalışan bu hizmeti veremiyor."));

                // 5. Tarih kontrolü (geçmiş tarih olamaz).
                //    Tek referans saati _dateTimeService.UtcNow'dur. İstek UTC instant/date
                //    olarak gelir; mesai saatleri işletmenin yerel duvar saatidir.
                var nowUtc = _dateTimeService.UtcNow;
                var businessNow = _dateTimeService.ToBusinessLocalTime(nowUtc);
                var date = _dateTimeService.ToBusinessLocalTime(request.Date).Date;

                if (date < businessNow.Date)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Geçmiş tarih için randevu alınamaz."));

                // 6. Çalışanın o günkü mesai saati yerel duvar saatinde slot adaylarını belirler.
                //    İşletme, çalışan ve izin kontrolleri her slotun TUM aralığı üzerinden
                //    aşağıda yapılır; gece yarısıyla IsOpenOn(date) çağrılmaz.
                var workingHour = employee.GetWorkingHourByDay(date.DayOfWeek);
                if (workingHour is null || workingHour.IsClosed)
                    return Result<List<AvailableSlotDto>>.Success(new List<AvailableSlotDto>());

                // 7. O günkü mevcut randevuları getir.
                //    Randevular DB'de UTC saklanır; önceki gün başlayıp bu gün taşan
                //    randevuların da kaçması için UTC penceresi bir gün genişletilir.
                //    Service include edilir: çakışma penceresi mevcut randevunun gerçek süresini kullanır.
                var windowStartUtc = _dateTimeService.ToUtc(date.AddDays(-1));
                var windowEndUtc = _dateTimeService.ToUtc(date.AddDays(1));
                var existingAppointments = await _appointmentRepository.FindWithIncludesAsync(
                    a => a.EmployeeId == request.EmployeeId &&
                         !a.IsDeleted &&
                         a.Status != EnumAppointmentStatus.Cancelled &&
                         a.AppointmentDate >= windowStartUtc &&
                         a.AppointmentDate < windowEndUtc,
                    new[] { nameof(Appointment.Service) },
                    cancellationToken);

                // 8. Hizmet süresi
                var duration = service.DurationInMinutes;

                // 9. Yerel duvar saatinde slot adayları oluştur; dönüş değerleri UTC olacak.
                var slots = new List<AvailableSlotDto>();
                var localSlotStart = date.Add(workingHour.StartTime);
                var localWorkingEnd = date.Add(workingHour.EndTime);

                while (localSlotStart.AddMinutes(duration) <= localWorkingEnd)
                {
                    var localSlotEnd = localSlotStart.AddMinutes(duration);

                    // Mesai, mola ve onaylı izin kontrolleri TUM aralık üzerinden yapılır.
                    var isWithinSchedule = business.IsOpenDuring(localSlotStart, localSlotEnd) &&
                                           employee.IsWorkingDuring(localSlotStart, localSlotEnd);
                    var isOnApprovedTimeOff = employee.IsOnApprovedTimeOff(localSlotStart, localSlotEnd);

                    // Randevular UTC öldüğündan çakışma ve geçmişlik UTC anlarında karşılaştırılır.
                    var slotStartUtc = _dateTimeService.ToUtc(localSlotStart);
                    var slotEndUtc = _dateTimeService.ToUtc(localSlotEnd);
                    var isPast = slotStartUtc < nowUtc;

                    // Fallback süre, istenen hizmetin değil mevcut randevunun hizmet süresidir;
                    // aksi halde farklı süreli hizmetlerde yanlış çakışma hesaplanır.
                    var hasConflict = existingAppointments.Any(a =>
                        a.OverlapsWith(slotStartUtc, slotEndUtc, a.Service?.DurationInMinutes ?? duration));

                    // Mesai dışı/izinli/geçmiş/çakışlı slotlar da IsAvailable=false ile dondurulur.
                    var isAvailable = isWithinSchedule && !isOnApprovedTimeOff && !isPast && !hasConflict;

                    slots.Add(new AvailableSlotDto
                    {
                        StartTime = slotStartUtc,
                        EndTime = slotEndUtc,
                        IsAvailable = isAvailable
                    });

                    // 15 dakika aralıkla slotları ilerlet
                    localSlotStart = localSlotStart.AddMinutes(15);
                }

                return Result<List<AvailableSlotDto>>.Success(slots);
            }
            catch (Exception)
            {
                // Hata detayı (ex.Message) istemciye sızdırılmaz.
                return Result<List<AvailableSlotDto>>.Failure(
                    Error.Internal("Müsait saatler hesaplanırken bir hata oluştu."));
            }
        }
    }
}