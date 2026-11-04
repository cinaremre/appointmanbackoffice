using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AppointMan.Infrastructure.Persistence.Repositories
{
    public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Appointment>> GetAppointmentsByBusinessAsync(
            Guid businessId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            EnumAppointmentStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(a => a.User)
                .Include(a => a.Service)
                .Where(a => a.BusinessId == businessId && !a.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(a => a.AppointmentDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.AppointmentDate <= endDate.Value);

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            return await query
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Appointment>> GetAppointmentsByUserAsync(
            Guid userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            EnumAppointmentStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(a => a.Business)
                .Include(a => a.Service)
                .Where(a => a.UserId == userId && !a.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(a => a.AppointmentDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.AppointmentDate <= endDate.Value);

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            return await query
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Appointment>> GetUpcomingAppointmentsByBusinessAsync(
            Guid businessId,
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.User)
                .Include(a => a.Service)
                .Where(a => a.BusinessId == businessId &&
                           !a.IsDeleted &&
                           a.AppointmentDate >= DateTime.UtcNow &&
                           a.Status == EnumAppointmentStatus.Confirmed)
                .OrderBy(a => a.AppointmentDate)
                .Take(count)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Appointment>> GetUpcomingAppointmentsByUserAsync(
            Guid userId,
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Business)
                .Include(a => a.Service)
                .Where(a => a.UserId == userId &&
                           !a.IsDeleted &&
                           a.AppointmentDate >= DateTime.UtcNow &&
                           a.Status == EnumAppointmentStatus.Confirmed)
                .OrderBy(a => a.AppointmentDate)
                .Take(count)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<bool> HasConflictAsync(
            Guid businessId,
            DateTime startTime,
            DateTime endTime,
            Guid? excludeAppointmentId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(a => a.BusinessId == businessId &&
                           !a.IsDeleted &&
                           a.Status != EnumAppointmentStatus.Cancelled &&
                           a.AppointmentDate < endTime &&
                           a.AppointmentDate.AddMinutes(a.Service.DurationInMinutes) > startTime);

            if (excludeAppointmentId.HasValue)
                query = query.Where(a => a.Id != excludeAppointmentId.Value);

            return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> GetAppointmentCountByBusinessAsync(
            Guid businessId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(a => a.BusinessId == businessId && !a.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(a => a.AppointmentDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.AppointmentDate <= endDate.Value);

            return await query.CountAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}