using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Interfaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<IReadOnlyList<Appointment>> GetAppointmentsByBusinessAsync(
            Guid businessId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            EnumAppointmentStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Appointment>> GetAppointmentsByUserAsync(
            Guid userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            EnumAppointmentStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Appointment>> GetUpcomingAppointmentsByBusinessAsync(
            Guid businessId,
            int count = 10,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Appointment>> GetUpcomingAppointmentsByUserAsync(
            Guid userId,
            int count = 10,
            CancellationToken cancellationToken = default);

        Task<bool> HasConflictAsync(
            Guid businessId,
            DateTime startTime,
            DateTime endTime,
            Guid? excludeAppointmentId = null,
            CancellationToken cancellationToken = default);

        Task<int> GetAppointmentCountByBusinessAsync(
            Guid businessId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);
    }
}