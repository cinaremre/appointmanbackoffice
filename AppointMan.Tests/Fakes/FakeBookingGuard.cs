using AppointMan.Application.Common.Abstractions;

namespace AppointMan.Tests.Fakes;

public sealed class FakeBookingGuard : IAppointmentBookingGuard
{
    private readonly List<(Guid EmployeeId, DateTime Start, DateTime End, Guid? ExcludeId)> _conflicts = new();

    public void AddConflict(Guid employeeId, DateTime start, DateTime end, Guid? excludeId = null)
        => _conflicts.Add((employeeId, start, end, excludeId));

    public Task<bool> HasConflictAsync(
        Guid employeeId,
        DateTime start,
        DateTime end,
        int fallbackDurationMinutes,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var hasConflict = _conflicts.Any(c =>
            c.EmployeeId == employeeId &&
            (c.ExcludeId is null || c.ExcludeId != excludeAppointmentId) &&
            c.Start < end &&
            c.End > start);

        return Task.FromResult(hasConflict);
    }
}
