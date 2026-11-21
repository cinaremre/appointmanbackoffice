using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointments
{
    /// <summary>
    /// Randevulari isletme, calisan, durum ve tarih araligina gore filtreleyip
    /// sayfalayarak listeler (dokuman Modul 12). Tarih karsilastirmalari
    /// UTC AppointmentDate uzerinde yapilir.
    /// </summary>
    public class GetAdminAppointmentsQueryHandler : IRequestHandler<GetAdminAppointmentsQuery, Result<AdminPagedResult<AdminAppointmentListItemDto>>>
    {
        /// <summary>Izin verilen en buyuk sayfa boyutu (dokuman 7.4).</summary>
        public const int MaxPageSize = 200;

        /// <summary>pageSize verilmediginde kullanilan varsayilan.</summary>
        public const int DefaultPageSize = 50;

        private readonly IRepository<Appointment> _appointmentRepository;

        public GetAdminAppointmentsQueryHandler(IRepository<Appointment> appointmentRepository)
        {
            _appointmentRepository = appointmentRepository;
        }

        public async Task<Result<AdminPagedResult<AdminAppointmentListItemDto>>> Handle(
            GetAdminAppointmentsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sayfa degerleri tek yerden sinirlanir.
                var page = Math.Max(request.Page, 1);
                var pageSize = Math.Clamp(
                    request.PageSize <= 0 ? DefaultPageSize : request.PageSize,
                    1,
                    MaxPageSize);

                // 2. Filtreler ifade uzerinde kurulur.
                var appointments = await _appointmentRepository.FindAsync(
                    BuildPredicate(request.BusinessId, request.EmployeeId, request.Status, request.FromDate, request.ToDate),
                    cancellationToken);

                // 3. En yeni randevu once olacak sekilde sirala ve sayfala.
                var ordered = appointments
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(ToListItem)
                    .ToList();

                return Result.Success(new AdminPagedResult<AdminAppointmentListItemDto>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = ordered.Count
                });
            }
            catch (Exception ex)
            {
                return Result.Failure<AdminPagedResult<AdminAppointmentListItemDto>>(
                    Error.Internal("Randevular listelenirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>
        /// Isletme, calisan, durum ve tarih araligi filtrelerini tek ifadeye toplar.
        /// Null filtreler kisayol degerlendirmesiyle atlanir.
        /// </summary>
        private static Expression<Func<Appointment, bool>> BuildPredicate(
            Guid? businessId,
            Guid? employeeId,
            EnumAppointmentStatus? status,
            DateTime? fromDate,
            DateTime? toDate)
            => a =>
                (businessId == null || a.BusinessId == businessId)
                && (employeeId == null || a.EmployeeId == employeeId)
                && (status == null || a.Status == status)
                && (fromDate == null || a.AppointmentDate >= fromDate)
                && (toDate == null || a.AppointmentDate <= toDate);

        /// <summary>Entity'yi liste ogesine cevirir.</summary>
        private static AdminAppointmentListItemDto ToListItem(Appointment appointment) => new()
        {
            Id = appointment.Id,
            BusinessId = appointment.BusinessId,
            UserId = appointment.UserId,
            EmployeeId = appointment.EmployeeId,
            ServiceId = appointment.ServiceId,
            AppointmentDate = appointment.AppointmentDate,
            Status = appointment.Status,
            Notes = appointment.Notes
        };
    }
}
