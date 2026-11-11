using System;
using System.Threading;
using System.Threading.Tasks;
using AppointMan.Application.Common.Abstractions;
using AppointMan.Domain.Enums;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AppointMan.Infrastructure.Persistence.Services
{
    /// <summary>
    /// Rezervasyon cakismasini SERIALIZABLE izolasyon altinda kontrol eder.
    /// SERIALIZABLE, ayni yuklem icin key-range kilidi kurdugundan iki paralel
    /// istegin ayni zaman araligina randevu eklemesi imkansizdir; check-then-act
    /// yarisi veritabani seviyesinde serilestirilir.
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
            CancellationToken cancellationToken = default)
        {
            // Soft delete global filtresi burada acikca uygulanir.
            return _context.Appointments
                .IgnoreQueryFilters()
                .AnyAsync(
                    a => a.EmployeeId == employeeId
                         && !a.IsDeleted
                         && a.Status != EnumAppointmentStatus.Cancelled
                         && a.AppointmentDate < end
                         && (a.EndTime ?? a.AppointmentDate.AddMinutes(fallbackDurationMinutes)) > start,
                    cancellationToken);
        }
    }
}
