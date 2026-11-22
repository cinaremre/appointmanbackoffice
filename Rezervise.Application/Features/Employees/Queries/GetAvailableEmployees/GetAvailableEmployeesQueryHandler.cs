using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Employees.Queries.GetAvailableEmployees
{
    public class GetAvailableEmployeesQueryHandler : IRequestHandler<GetAvailableEmployeesQuery, Result<List<EmployeeDto>>>
    {
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IDateTimeService _dateTimeService;

        public GetAvailableEmployeesQueryHandler(
            IRepository<Employee> employeeRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<Service> serviceRepository,
            IRepository<Business> businessRepository,
            IDateTimeService dateTimeService)
        {
            _employeeRepository = employeeRepository;
            _appointmentRepository = appointmentRepository;
            _serviceRepository = serviceRepository;
            _businessRepository = businessRepository;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<List<EmployeeDto>>> Handle(GetAvailableEmployeesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletmenin aktif ve randevu kabul eden calisanlarini getir.
                //    Navigasyonlar yuklenmezse domain kontrolleri sessizce yanlis sonuc verir:
                //    CanProvideService() her zaman false doner ve sonuc listesi bosalir.
                var employees = await _employeeRepository.FindWithIncludesAsync(
                    e => e.BusinessId == request.BusinessId &&
                         e.IsActive &&
                         e.IsAvailableForBooking &&
                         !e.IsDeleted,
                    new[]
                    {
                        nameof(Employee.EmployeeServices),   // CanProvideService
                        nameof(Employee.WorkingHours),       // IsWorkingOn
                        nameof(Employee.TimeOffs),           // IsOnTimeOff
                        nameof(Employee.Business)            // BusinessName
                    },
                    cancellationToken);

                var availableEmployees = employees.ToList();

                // 2. Hizmet filtresi
                if (request.ServiceId.HasValue)
                {
                    availableEmployees = availableEmployees
                        .Where(e => e.CanProvideService(request.ServiceId.Value))
                        .ToList();
                }

                // 3. Tarih filtresi - calisma saati, izin ve cakisma kontrolu
                if (request.AppointmentDate.HasValue && request.ServiceId.HasValue)
                {
                    var service = await _serviceRepository.GetByIdAsync(request.ServiceId.Value, cancellationToken);
                    if (service is null)
                        return Result<List<EmployeeDto>>.Failure(Error.NotFound("Hizmet", request.ServiceId.Value.ToString()));

                    // Isletme mesaisi yalnizca calisanin kendi mesai kaydi yoksa devreye girer.
                    var businesses = await _businessRepository.FindWithIncludesAsync(
                        b => b.Id == request.BusinessId && !b.IsDeleted,
                        new[] { nameof(Business.WorkingHours) },
                        cancellationToken);

                    var business = businesses.FirstOrDefault();

                    var appointmentDate = request.AppointmentDate.Value;

                    // The API receives UTC instants, but opening hours are stored as local
                    // wall-clock time-of-day. Comparing against the raw UTC value would shift
                    // every availability decision by the configured offset.
                    var businessLocalStart = _dateTimeService.ToBusinessLocalTime(appointmentDate);
                    var businessLocalEnd = businessLocalStart.AddMinutes(service.DurationInMinutes);

                    // Conflict detection keeps using UTC because appointments are persisted in UTC.
                    var startTime = appointmentDate;
                    var endTime = startTime.AddMinutes(service.DurationInMinutes);

                    // Calisma saati ve izin kontrolleri bellekte yapilir; navigasyonlar zaten yuklu.
                    var scheduleEligible = availableEmployees
                        .Where(e => IsScheduled(e, business, businessLocalStart, businessLocalEnd))
                        .ToList();

                    if (scheduleEligible.Count == 0)
                    {
                        availableEmployees = scheduleEligible;
                    }
                    else
                    {
                        // Cakisma kontrolu tek sorguda yapilir: calisan basina sorgu (N+1) yerine 1 sorgu.
                        var candidateIds = scheduleEligible.Select(e => e.Id).ToList();

                        // No-show ve iptal edilmis randevular saati isgal etmez.
                        var conflicts = await _appointmentRepository.FindAsync(
                            a => candidateIds.Contains(a.EmployeeId) &&
                                 a.Status != Domain.Enums.EnumAppointmentStatus.Cancelled &&
                                 a.Status != Domain.Enums.EnumAppointmentStatus.NoShow &&
                                 a.AppointmentDate < endTime &&
                                 a.AppointmentDate.AddMinutes(a.Service.DurationInMinutes) > startTime,
                            cancellationToken);

                        var busyEmployeeIds = conflicts.Select(a => a.EmployeeId).ToHashSet();

                        availableEmployees = scheduleEligible
                            .Where(e => !busyEmployeeIds.Contains(e.Id))
                            .ToList();
                    }
                }

                // 4. DTO'ya donustur
                var dtos = availableEmployees
                    .OrderBy(e => e.DisplayOrder)
                    .Select(e => new EmployeeDto
                    {
                        Id = e.Id,
                        FirstName = e.FirstName,
                        LastName = e.LastName,
                        Title = e.Title,
                        Bio = e.Bio,
                        ProfilePictureUrl = e.ProfilePictureUrl,
                        PhoneNumber = e.PhoneNumber.Value,
                        Email = e.Email?.Value,
                        BusinessId = e.BusinessId,
                        BusinessName = e.Business?.Name,
                        IsActive = e.IsActive,
                        IsAvailableForBooking = e.IsAvailableForBooking,
                        DisplayOrder = e.DisplayOrder,
                        CreatedAt = e.CreatedAt,
                        FullName = e.GetFullName(),
                        DisplayName = e.GetDisplayName()
                    })
                    .ToList();

                return Result<List<EmployeeDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<EmployeeDto>>.Failure(
                    Error.Internal("Musait calisanlar getirilirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>
        /// Calisanin verilen yerel zaman araliginda gorevli olup olmadigini belirler.
        /// Calisanin kendi mesai kaydi yoksa isletme mesaisi esas alinir; aksi halde
        /// mesai kaydi olmayan calisanlar hicbir zaman musait donmez.
        /// </summary>
        /// <param name="employee">Kontrol edilecek calisan.</param>
        /// <param name="business">Isletme (mesai saatleri yuklu olmali).</param>
        /// <param name="localStart">Isletme yerel saatinde aralik baslangici.</param>
        /// <param name="localEnd">Isletme yerel saatinde aralik bitisi.</param>
        /// <returns>Calisan bu araligi verebiliyorsa true.</returns>
        private static bool IsScheduled(Employee employee, Business? business, DateTime localStart, DateTime localEnd)
        {
            if (employee.IsOnTimeOff(localStart))
                return false;

            return employee.WorkingHours.Count > 0
                ? employee.IsWorkingOn(localStart)
                : business?.IsOpenDuring(localStart, localEnd) == true;
        }
    }
}