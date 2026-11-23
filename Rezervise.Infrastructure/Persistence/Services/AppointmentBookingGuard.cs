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
    /// Rezervasyon çakışma kontrolünü yapar. İzolasyon seviyesini bu metot değiştirmez;
    /// arayan SERIALIZABLE transaction içinde çağırmalıdır. Bu koşul sağlandığında
    /// aralık kilidi sayesinde iki paralel isteğin aynı zaman aralığına randevu
    /// eklemesi engellenir ve check-then-act yarısı serileştirilir.
    /// </summary>
    public sealed class AppointmentBookingGuard : IAppointmentBookingGuard
    {
        private readonly AppDbContext _context;

        public AppointmentBookingGuard(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Çakışan aktif randevu olup olmadığını belirler.
        /// Bitiş saati boş olan randevular için servis süresi kullanılır;
        /// bu formül Domain tarafındaki OverlapsWith ile aynıdır.
        /// </summary>
        public Task<bool> HasConflictAsync(
            Guid employeeId,
            DateTime start,
            DateTime end,
            int fallbackDurationMinutes,
            Guid? excludeAppointmentId = null,
            CancellationToken cancellationToken = default)
        {
            // Soft delete global filtresi IgnoreQueryFilters ile kaldırıldığı için
            // silinmemiş kayıt koşulu açıkça uygulanır. Null exclude id SQL'de
            // "<> NULL" ile tüm satırları elediğinden HasValue ile korunur.
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
