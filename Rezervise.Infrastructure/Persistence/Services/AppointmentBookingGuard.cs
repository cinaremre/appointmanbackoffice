using System;
using System.Threading;
using System.Threading.Tasks;
using Rezervise.Application.Common.Abstractions;
using Rezervise.Domain.Enums;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Rezervise.Infrastructure.Persistence.Services
{
    /// <summary>
    /// Rezervasyon cakisma kontrolunu yapar. Izolasyon seviyesini bu metot degistirmez;
    /// arayan SERIALIZABLE transaction icinde cagirmalidir. Bu kosul saglandiginda
    /// aralik kilidi sayesinde iki paralel istegin ayni zaman araligina randevu
    /// eklemesi engellenir ve check-then-act yarisi serilestirilir.
    /// </summary>
    public sealed class AppointmentBookingGuard : IAppointmentBookingGuard
    {
        private readonly AppDbContext _context;

        public AppointmentBookingGuard(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cakisan aktif randevu olup olmadigini belirler.
        /// Bitis saati bos olan randevular icin servis suresi kullanilir;
        /// bu formul Domain tarafindaki OverlapsWith ile aynidir.
        /// </summary>
        public Task<bool> HasConflictAsync(
            Guid employeeId,
            DateTime start,
            DateTime end,
            int fallbackDurationMinutes,
            Guid? excludeAppointmentId = null,
            CancellationToken cancellationToken = default)
        {
            // Soft delete global filtresi IgnoreQueryFilters ile kaldirildigi icin
            // silinmemis kayit kosulu acikca uygulanir. Null exclude id SQL'de
            // "<> NULL" ile tum satirlari elediginden HasValue ile korunur.
            return _context.Appointments
                .IgnoreQueryFilters()
                .AnyAsync(
                    a => a.EmployeeId == employeeId
                         && !a.IsDeleted
                         && a.Status != EnumAppointmentStatus.Cancelled
                         && (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value)
                         && a.AppointmentDate < end
                         && (a.EndTime ?? a.AppointmentDate.AddMinutes(fallbackDurationMinutes)) > start,
                    cancellationToken);
        }
    }
}
