using AppointMan.Application.Common.Interfaces;

namespace AppointMan.Tests.Fakes;

/// <summary>
/// Denetim kaydi yazimini kayda gecen test saplamasi.
/// Handler'larin hangi islemi hangi gerekceyle kaydettigini
/// assert etmek icin kullanilir.
/// </summary>
public sealed class TestAdminAuditSink : IAdminAuditSink
{
    public sealed record AuditEntry(
        Guid ActorUserId,
        string ActorRole,
        Guid? BusinessId,
        string EntityType,
        string EntityId,
        string Action,
        string? OldValues,
        string? NewValues,
        string? Reason,
        string? CorrelationId,
        string? IpAddress);

    public List<AuditEntry> Entries { get; } = new();

    public Task RecordAsync(
        Guid actorUserId,
        string actorRole,
        Guid? businessId,
        string entityType,
        string entityId,
        string action,
        string? oldValues = null,
        string? newValues = null,
        string? reason = null,
        string? correlationId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        Entries.Add(new AuditEntry(
            actorUserId,
            actorRole,
            businessId,
            entityType,
            entityId,
            action,
            oldValues,
            newValues,
            reason,
            correlationId,
            ipAddress));

        return Task.CompletedTask;
    }
}
