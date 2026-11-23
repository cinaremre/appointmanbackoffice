using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointments
{
    /// <summary>
    /// Randevuları işletme, çalışan, durum ve tarih aralığına göre filtreleyip
    /// sayfalayarak listeler (doküman Modül 12). Tarih karşılaştırmaları
    /// UTC AppointmentDate üzerinde yapılır.
    /// </summary>
    public class GetAdminAppointmentsQueryHandler : IRequestHandler<GetAdminAppointmentsQuery, Result<AdminPagedResult<AdminAppointmentListItemDto>>>
    {
        /// <summary>İzin verilen en büyük sayfa boyutu (doküman 7.4).</summary>
        public const int MaxPageSize = 200;

        /// <summary>pageSize verilmediğinde kullanılan varsayılan.</summary>
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
                // 1. Sayfa değerleri tek yerden sınırlanır.
                var page = Math.Max(request.Page, 1);
                var pageSize = Math.Clamp(
                    request.PageSize <= 0 ? DefaultPageSize : request.PageSize,
                    1,
                    MaxPageSize);

                // 2. Filtreler ifade üzerinde kurulur.
                var appointments = await _appointmentRepository.FindAsync(
                    BuildPredicate(request.BusinessId, request.EmployeeId, request.Status, request.FromDate, request.ToDate),
                    cancellationToken);

                // 3. En yeni randevu önce olacak şekilde sırala ve sayfala.
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
                    Error.Internal("Randevular listelenirken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>
        /// İşletme, çalışan, durum ve tarih aralığı filtrelerini tek ifadeye toplar.
        /// Null filtreler kısayol değerlendirmesiyle atlanır.
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

        /// <summary>Entity'yi liste öğesine çevirir.</summary>
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
