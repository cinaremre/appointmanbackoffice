using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentConflicts
{
    /// <summary>
    /// Verilen isletmenin verilen gunundeki calisan cakismalarini tespit eder.
    /// Aktif randevular (Pending/Confirmed) EmployeeId + AppointmentDate
    /// ciftine gore gruplanir; birden fazla kayit dusen gruplar doner.
    /// </summary>
    public class GetAdminAppointmentConflictsQueryHandler : IRequestHandler<GetAdminAppointmentConflictsQuery, Result<IReadOnlyList<AdminAppointmentConflictDto>>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;

        public GetAdminAppointmentConflictsQueryHandler(IRepository<Appointment> appointmentRepository)
        {
            _appointmentRepository = appointmentRepository;
        }

        public async Task<Result<IReadOnlyList<AdminAppointmentConflictDto>>> Handle(
            GetAdminAppointmentConflictsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Gun sinirlari UTC olarak hesaplanir; saat/dakika bilgisi
                //    istekten bagimsiz olarak sifirlanir.
                var dayStart = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
                var dayEnd = dayStart.AddDays(1);

                // 2. O gunun aktif randevularini getir.
                var activeAppointments = await _appointmentRepository.FindAsync(
                    a => a.BusinessId == request.BusinessId
                        && a.AppointmentDate >= dayStart
                        && a.AppointmentDate < dayEnd
                        && (a.Status == EnumAppointmentStatus.Pending
                            || a.Status == EnumAppointmentStatus.Confirmed),
                    cancellationToken);

                // 3. Ayni calisan + ayni randevu saati gruplarini tespit et.
                var conflicts = activeAppointments
                    .GroupBy(a => new { a.EmployeeId, a.AppointmentDate })
                    .Where(g => g.Count() > 1)
                    .Select(g => new AdminAppointmentConflictDto
                    {
                        BusinessId = request.BusinessId,
                        EmployeeId = g.Key.EmployeeId,
                        AppointmentDate = g.Key.AppointmentDate,
                        Count = g.Count(),
                        AppointmentIds = g.Select(a => a.Id).OrderBy(id => id).ToList()
                    })
                    .OrderBy(c => c.AppointmentDate)
                    .ThenBy(c => c.EmployeeId)
                    .ToList();

                return Result.Success((IReadOnlyList<AdminAppointmentConflictDto>)conflicts);
            }
            catch (Exception ex)
            {
                return Result.Failure<IReadOnlyList<AdminAppointmentConflictDto>>(
                    Error.Internal("Randevu cakismalari getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
