using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Reporting.Queries.GetBusinessDashboard
{
    public class GetBusinessDashboardQueryHandler : IRequestHandler<GetBusinessDashboardQuery, Result<BusinessDashboardDto>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<WorkingHour> _workingHourRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly ICurrentUserService _currentUser;

        public GetBusinessDashboardQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<WorkingHour> workingHourRepository,
            IRepository<Employee> employeeRepository,
            IRepository<Service> serviceRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _appointmentRepository = appointmentRepository;
            _workingHourRepository = workingHourRepository;
            _employeeRepository = employeeRepository;
            _serviceRepository = serviceRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<BusinessDashboardDto>> Handle(GetBusinessDashboardQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<BusinessDashboardDto>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki: dashboard isletme sahibi veya platform yoneticisine aittir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<BusinessDashboardDto>.Failure(Error.Unauthorized("Bu isletmenin dashboardunu gorme yetkiniz yok."));

                // 3. Gun araligi: [gun basi, gun sonu) UTC.
                var dayStart = request.Date.Date;
                var dayEnd = dayStart.AddDays(1);

                // 4. Gunun randevularini tek sorguda getir.
                var dayAppointments = await _appointmentRepository.FindAsync(
                    a => a.BusinessId == request.BusinessId
                        && a.AppointmentDate >= dayStart
                        && a.AppointmentDate < dayEnd
                        && !a.IsDeleted,
                    cancellationToken);

                var dto = new BusinessDashboardDto
                {
                    BusinessId = request.BusinessId,
                    Date = dayStart,
                    BugunkiRandevular = dayAppointments.Count(a =>
                        a.Status == EnumAppointmentStatus.Pending
                        || a.Status == EnumAppointmentStatus.Confirmed),
                    Tamamlanan = dayAppointments.Count(a => a.Status == EnumAppointmentStatus.Completed),
                    Iptaller = dayAppointments.Count(a => a.Status == EnumAppointmentStatus.Cancelled),
                    NoShow = dayAppointments.Count(a => a.Status == EnumAppointmentStatus.NoShow)
                };

                // 5. Doluluk orani (basit ilk surum):
                //    dolu sure = tamamlanan + aktif (Pending/Confirmed) randevularin toplam suresi
                //    kapasite = aktif calisan sayisi * gunun toplam acik saati
                var occupiedMinutes = 0.0;
                var services = await _serviceRepository.FindAsync(
                    s => s.BusinessId == request.BusinessId && !s.IsDeleted,
                    cancellationToken);

                var serviceDurations = services.ToDictionary(s => s.Id, s => (double)s.DurationInMinutes);

                foreach (var appointment in dayAppointments)
                {
                    var isOccupying = appointment.Status == EnumAppointmentStatus.Pending
                        || appointment.Status == EnumAppointmentStatus.Confirmed
                        || appointment.Status == EnumAppointmentStatus.Completed;

                    if (!isOccupying)
                        continue;

                    // Bitis saati varsa onu kullan; yoksa hizmet suresine dus.
                    var minutes = appointment.EndTime.HasValue
                        ? (appointment.EndTime.Value - appointment.AppointmentDate).TotalMinutes
                        : serviceDurations.GetValueOrDefault(appointment.ServiceId);

                    occupiedMinutes += Math.Max(minutes, 0);
                }

                var employeeCount = await _employeeRepository.CountAsync(
                    e => e.BusinessId == request.BusinessId && e.IsActive && !e.IsDeleted,
                    cancellationToken);

                var workingHours = await _workingHourRepository.FindAsync(
                    w => w.BusinessId == request.BusinessId
                        && w.DayOfWeek == dayStart.DayOfWeek
                        && !w.IsClosed
                        && !w.IsDeleted,
                    cancellationToken);

                var openMinutes = workingHours.Sum(w => w.GetDuration().TotalMinutes);
                var capacityMinutes = employeeCount * openMinutes;

                // Calisan veya acik saat yoksa doluluk hesaplanamaz; 0 doner.
                dto.DolulukOrani = capacityMinutes > 0
                    ? Math.Round((decimal)(occupiedMinutes / capacityMinutes), 2, MidpointRounding.AwayFromZero)
                    : 0m;

                return Result<BusinessDashboardDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<BusinessDashboardDto>.Failure(
                    Error.Internal("Dashboard getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
