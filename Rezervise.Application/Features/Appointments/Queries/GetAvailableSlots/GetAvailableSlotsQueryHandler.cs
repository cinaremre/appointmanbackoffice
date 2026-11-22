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
                //    Tek referans saati _dateTimeService.UtcNow'dur. Istek UTC instant/date
                //    olarak gelir; mesai saatleri isletmenin yerel duvar saatidir.
                var nowUtc = _dateTimeService.UtcNow;
                var businessNow = _dateTimeService.ToBusinessLocalTime(nowUtc);
                var date = _dateTimeService.ToBusinessLocalTime(request.Date).Date;

                if (date < businessNow.Date)
                    return Result<List<AvailableSlotDto>>.Failure(Error.BusinessRule("Gecmis tarih icin randevu alinamaz."));

                // 6. Calisanin o gunku mesai saati yerel duvar saatinde slot adaylarini belirler.
                //    Isletme, calisan ve izin kontrolleri her slotun TUM araligi uzerinden
                //    asagida yapilir; gece yarisiyla IsOpenOn(date) cagrilmaz.
                var workingHour = employee.GetWorkingHourByDay(date.DayOfWeek);
                if (workingHour is null || workingHour.IsClosed)
                    return Result<List<AvailableSlotDto>>.Success(new List<AvailableSlotDto>());

                // 7. O gunku mevcut randevulari getir.
                //    Randevular DB'de UTC saklanir; onceki gun baslayip bu gun tasan
                //    randevularin da kacmasi icin UTC penceresi bir gun genisletilir.
                //    Service include edilir: cakisma penceresi mevcut randevunun gercek suresini kullanir.
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

                // 8. Hizmet suresi
                var duration = service.DurationInMinutes;

                // 9. Yerel duvar saatinde slot adaylari olustur; donus degerleri UTC olacak.
                var slots = new List<AvailableSlotDto>();
                var localSlotStart = date.Add(workingHour.StartTime);
                var localWorkingEnd = date.Add(workingHour.EndTime);

                while (localSlotStart.AddMinutes(duration) <= localWorkingEnd)
                {
                    var localSlotEnd = localSlotStart.AddMinutes(duration);

                    // Mesai, mola ve onayli izin kontrolleri TUM aralik uzerinden yapilir.
                    var isWithinSchedule = business.IsOpenDuring(localSlotStart, localSlotEnd) &&
                                           employee.IsWorkingDuring(localSlotStart, localSlotEnd);
                    var isOnApprovedTimeOff = employee.IsOnApprovedTimeOff(localSlotStart, localSlotEnd);

                    // Randevular UTC oldugundan cakisma ve gecmislik UTC anlarinda karsilastirilir.
                    var slotStartUtc = _dateTimeService.ToUtc(localSlotStart);
                    var slotEndUtc = _dateTimeService.ToUtc(localSlotEnd);
                    var isPast = slotStartUtc < nowUtc;

                    // Fallback sure, istenen hizmetin degil mevcut randevunun hizmet suresidir;
                    // aksi halde farkli sureli hizmetlerde yanlis cakisma hesaplanir.
                    var hasConflict = existingAppointments.Any(a =>
                        a.OverlapsWith(slotStartUtc, slotEndUtc, a.Service?.DurationInMinutes ?? duration));

                    // Mesai disi/izinli/gecmis/cakisli slotlar da IsAvailable=false ile dondurulur.
                    var isAvailable = isWithinSchedule && !isOnApprovedTimeOff && !isPast && !hasConflict;

                    slots.Add(new AvailableSlotDto
                    {
                        StartTime = slotStartUtc,
                        EndTime = slotEndUtc,
                        IsAvailable = isAvailable
                    });

                    // 15 dakika aralikla slotlari ilerlet
                    localSlotStart = localSlotStart.AddMinutes(15);
                }

                return Result<List<AvailableSlotDto>>.Success(slots);
            }
            catch (Exception)
            {
                // Hata detayi (ex.Message) istemciye sizdirilmaz.
                return Result<List<AvailableSlotDto>>.Failure(
                    Error.Internal("Musait saatler hesaplanirken bir hata olustu."));
            }
        }
    }
}