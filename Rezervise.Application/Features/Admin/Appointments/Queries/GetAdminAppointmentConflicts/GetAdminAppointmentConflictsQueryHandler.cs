using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentConflicts
{
    /// <summary>
    /// Verilen işletmenin verilen günündeki çalışan çakışmalarını tespit eder.
    /// Aktif randevular (Pending/Confirmed) EmployeeId + AppointmentDate
    /// çiftine göre gruplanır; birden fazla kayıt düşen gruplar döner.
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
                // 1. Gün sınırları UTC olarak hesaplanır; saat/dakika bilgisi
                //    istekten bağımsız olarak sıfırlanır.
                var dayStart = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
                var dayEnd = dayStart.AddDays(1);

                // 2. O günün aktif randevularını getir.
                var activeAppointments = await _appointmentRepository.FindAsync(
                    a => a.BusinessId == request.BusinessId
                        && a.AppointmentDate >= dayStart
                        && a.AppointmentDate < dayEnd
                        && (a.Status == EnumAppointmentStatus.Pending
                            || a.Status == EnumAppointmentStatus.Confirmed),
                    cancellationToken);

                // 3. Aynı çalışan + aynı randevu saati gruplarını tespit et.
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
                    Error.Internal("Randevu çakışmaları getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
