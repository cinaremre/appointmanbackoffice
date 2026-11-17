using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Queries.GetAvailableSlots
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
                // 1. Isletme kontrolu.
                //    WorkingHours yuklenmezse IsOpenOn() yanlis sonuc verir.
                var businesses = await _businessRepository.FindWithIncludesAsync(
                    b => b.Id == request.BusinessId && !b.IsDeleted,
                    new[] { nameof(Business.WorkingHours) },
                    cancellationToken);

                var business = businesses.FirstOrDefault();
                if (business is null)
                    return Result<List<AvailableSlotDto>>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                if (!business.IsActive)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Isletme aktif degil."));

                // 2. Calisan kontrolu.
                //    Navigasyonlar yuklenmezse domain kontrolleri sessizce yanlis sonuc verir:
                //    CanProvideService() her zaman false doner (400) ve calisma saati bulunamaz (bos liste).
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
                    return Result<List<AvailableSlotDto>>.Failure(Error.NotFound("Calisan", request.EmployeeId.ToString()));

                if (!employee.IsActive)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Calisan aktif degil."));

                if (employee.BusinessId != request.BusinessId)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Calisan bu isletmeye ait degil."));

                // 3. Hizmet kontrolu
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null)
                    return Result<List<AvailableSlotDto>>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                if (!service.IsActive)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Hizmet aktif degil."));

                if (service.BusinessId != request.BusinessId)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Hizmet bu isletmeye ait degil."));

                // 4. Calisan bu hizmeti verebiliyor mu?
                if (!employee.CanProvideService(request.ServiceId))
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Calisan bu hizmeti veremiyor."));

                // 5. Tarih kontrolu (gecmis tarih olamaz).
                //    Istek UTC gelir, mesai saatleri yerel duvar saatidir; gun ve saat
                //    karsilastirmalari isletmenin yerel saatinde yapilir.
                var businessNow = _dateTimeService.ToBusinessLocalTime(DateTime.UtcNow);
                var date = _dateTimeService.ToBusinessLocalTime(request.Date).Date;

                if (date < businessNow.Date)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Gecmis tarih icin randevu alinamaz."));

                // 6. Isletme o gun acik mi? (Business.WorkingHours yukarida include edildi)
                if (!business.IsOpenOn(date))
                    return Result<List<AvailableSlotDto>>.Success(new List<AvailableSlotDto>());

                // 7. Calisan o gun calisiyor mu?
                var workingHour = employee.GetWorkingHourByDay(date.DayOfWeek);
                if (workingHour is null || workingHour.IsClosed)
                    return Result<List<AvailableSlotDto>>.Success(new List<AvailableSlotDto>());

                // 7. Calisan o gun izinde mi?
                if (employee.IsOnTimeOff(date))
                    return Result<List<AvailableSlotDto>>.Success(new List<AvailableSlotDto>());

                // 8. O gunku mevcut randevulari getir.
                //    Randevular DB'de UTC saklandigi icin yerel gun araligi UTC'ye cevrilir.
                //    Service include edilir: cakisma penceresi mevcut randevunun gercek suresini kullanir.
                var dayStart = _dateTimeService.ToUtc(date);
                var dayEnd = _dateTimeService.ToUtc(date.AddDays(1));
                var existingAppointments = await _appointmentRepository.FindWithIncludesAsync(
                    a => a.EmployeeId == request.EmployeeId &&
                         a.Status != EnumAppointmentStatus.Cancelled &&
                         a.AppointmentDate >= dayStart &&
                         a.AppointmentDate < dayEnd,
                    new[] { nameof(Appointment.Service) },
                    cancellationToken);

                // 9. Hizmet suresi
                var duration = service.DurationInMinutes;

                // 10. Calisma saatleri arasinda slotlari olustur
                var slots = new List<AvailableSlotDto>();
                var slotStart = date.Add(workingHour.StartTime);
                var slotEnd = date.Add(workingHour.EndTime);

                // Mola varsa, mola araligini atla
                var hasBreak = workingHour.BreakStartTime.HasValue && workingHour.BreakEndTime.HasValue;
                var breakStart = hasBreak ? date.Add(workingHour.BreakStartTime.Value) : DateTime.MinValue;
                var breakEnd = hasBreak ? date.Add(workingHour.BreakEndTime.Value) : DateTime.MinValue;

                while (slotStart.AddMinutes(duration) <= slotEnd)
                {
                    var candidateEnd = slotStart.AddMinutes(duration);

                    // Mola kontrolu
                    var isInBreak = hasBreak && slotStart < breakEnd && candidateEnd > breakStart;

                    // Gecmis zaman kontrolu (bugun icin), yerel saatle karsilastirilir
                    var isPast = slotStart < businessNow;

                    // Cakisma kontrolu.
                    //    Fallback sure, istenen hizmetin degil mevcut randevunun hizmet suresidir;
                    //    aksi halde farkli sureli hizmetlerde yanlis cakisma hesaplanir.
                    var hasConflict = existingAppointments.Any(a =>
                    {
                        var aStart = a.AppointmentDate;
                        var existingDuration = a.Service?.DurationInMinutes ?? duration;
                        var aEnd = a.EndTime ?? aStart.AddMinutes(existingDuration);
                        return aStart < candidateEnd && aEnd > slotStart;
                    });

                    var isAvailable = !isInBreak && !isPast && !hasConflict;

                    slots.Add(new AvailableSlotDto
                    {
                        StartTime = slotStart,
                        EndTime = candidateEnd,
                        IsAvailable = isAvailable
                    });

                    // 15 dakika aralikla slotlari ilerlet
                    slotStart = slotStart.AddMinutes(15);
                }

                return Result<List<AvailableSlotDto>>.Success(slots);
            }
            catch (Exception ex)
            {
                return Result<List<AvailableSlotDto>>.Failure(
                    Error.Internal("Musait saatler hesaplanirken bir hata olustu.", ex.Message));
            }
        }
    }
}