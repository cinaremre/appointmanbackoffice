using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Features.Admin.Notifications.Commands.RetryAdminNotification;
using AppointMan.Application.Features.Admin.Notifications.Queries.GetAdminNotificationStats;
using AppointMan.Application.Features.Admin.Notifications.Queries.GetAdminNotifications;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application.BackOffice;

/// <summary>
/// Backoffice bildirim yonetimi (dokuman Modul 14): liste filtreleri,
/// sayfalama kirpanma, 30 gunluk istatistik ve yeniden gonderim talebi
/// (varlik dogrulama + denetim kaydi).
/// </summary>
public class AdminNotificationTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private static readonly Guid AdminId = Guid.NewGuid();

    // ===== Liste filtreleri =====

    [Fact]
    public async Task GetNotifications_ShouldFilterByUser()
    {
        var notifications = new FakeRepository<Notification>(new[]
        {
            NewNotification(UserId, EnumNotificationType.AppointmentReminder),
            NewNotification(UserId, EnumNotificationType.Promotional),
            NewNotification(OtherUserId, EnumNotificationType.Promotional)
        });

        var sut = new GetAdminNotificationsQueryHandler(notifications);

        var result = await sut.Handle(new GetAdminNotificationsQuery { UserId = UserId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.All(result.Value.Items, n => Assert.Equal(UserId, n.UserId));
    }

    [Fact]
    public async Task GetNotifications_ShouldFilterByType()
    {
        var notifications = new FakeRepository<Notification>(new[]
        {
            NewNotification(UserId, EnumNotificationType.AppointmentReminder),
            NewNotification(UserId, EnumNotificationType.AppointmentReminder),
            NewNotification(UserId, EnumNotificationType.Promotional)
        });

        var sut = new GetAdminNotificationsQueryHandler(notifications);

        var result = await sut.Handle(
            new GetAdminNotificationsQuery { Type = EnumNotificationType.Promotional },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(EnumNotificationType.Promotional, item.Type);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetNotifications_ShouldClampPageAndPageSize()
    {
        var notifications = new FakeRepository<Notification>(new[]
        {
            NewNotification(UserId, EnumNotificationType.AppointmentReminder),
            NewNotification(UserId, EnumNotificationType.Promotional)
        });

        var sut = new GetAdminNotificationsQueryHandler(notifications);

        // Gecersiz sayfa 1'e, asiri buyuk sayfa boyutu 200'e kirpanir.
        var clamped = await sut.Handle(
            new GetAdminNotificationsQuery { Page = 0, PageSize = 500 },
            CancellationToken.None);

        Assert.True(clamped.IsSuccess);
        Assert.Equal(1, clamped.Value!.Page);
        Assert.Equal(200, clamped.Value.PageSize);
        Assert.Equal(2, clamped.Value.TotalCount);

        // Bos sayfa boyutu varsayilan 50'ye kirpanir.
        var defaulted = await sut.Handle(
            new GetAdminNotificationsQuery { Page = 1, PageSize = 0 },
            CancellationToken.None);

        Assert.True(defaulted.IsSuccess);
        Assert.Equal(50, defaulted.Value!.PageSize);
    }

    // ===== Istatistikler =====

    [Fact]
    public async Task GetStats_ShouldReportCountsAndTypeBreakdown()
    {
        var now = DateTime.UtcNow;

        var readReminder = NewNotification(UserId, EnumNotificationType.AppointmentReminder, read: true, sentAt: now.AddDays(-5));
        var readReminderOld = NewNotification(UserId, EnumNotificationType.AppointmentReminder, read: true, sentAt: now.AddDays(-3));
        var unreadPromotional = NewNotification(UserId, EnumNotificationType.Promotional, sentAt: now.AddDays(-1));

        // 30 gun penceresinin disindaki bildirim sayilmaz.
        var outOfWindow = NewNotification(
            UserId, EnumNotificationType.System, sentAt: now.AddDays(-40));

        var notifications = new FakeRepository<Notification>(new[]
        {
            readReminder, readReminderOld, unreadPromotional, outOfWindow
        });

        var sut = new GetAdminNotificationStatsQueryHandler(notifications);

        var result = await sut.Handle(new GetAdminNotificationStatsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Total);
        Assert.Equal(2, result.Value.ReadCount);
        Assert.Equal(1, result.Value.UnreadCount);

        var reminderStat = result.Value.ByType.Single(t => t.Type == EnumNotificationType.AppointmentReminder);
        var promotionalStat = result.Value.ByType.Single(t => t.Type == EnumNotificationType.Promotional);

        Assert.Equal(2, reminderStat.Count);
        Assert.Equal(1, promotionalStat.Count);

        // Saglayici maliyet aciklamasi doc yorumuyla birlikte doner.
        Assert.False(string.IsNullOrWhiteSpace(result.Value.CostNote));
    }

    // ===== Yeniden gonderim talebi =====

    [Fact]
    public async Task Retry_ShouldFailNotFound_WhenNotificationMissing()
    {
        var sink = new FakeAuditSink();

        var sut = new RetryAdminNotificationCommandHandler(
            new FakeRepository<Notification>(), sink, new FakeAuditContext());

        var result = await sut.Handle(new RetryAdminNotificationCommand
        {
            NotificationId = Guid.NewGuid(),
            ActorUserId = AdminId,
            ActorRole = "PlatformAdmin"
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
        Assert.Empty(sink.Records);
    }

    [Fact]
    public async Task Retry_ShouldFailUnauthorized_WhenActorUnresolved()
    {
        var notification = NewNotification(UserId, EnumNotificationType.AppointmentReminder);
        var sink = new FakeAuditSink();

        var sut = new RetryAdminNotificationCommandHandler(
            new FakeRepository<Notification>(new[] { notification }),
            sink,
            new FakeAuditContext());

        var result = await sut.Handle(new RetryAdminNotificationCommand
        {
            NotificationId = notification.Id,
            ActorUserId = Guid.Empty,
            ActorRole = "PlatformAdmin"
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        Assert.Empty(sink.Records);
    }

    [Fact]
    public async Task Retry_ShouldSucceedAndWriteAudit_WhenNotificationExists()
    {
        // Okunmamis bildirim: OldValues IsRead=false olarak denetim kaydina gecer.
        var notification = NewNotification(UserId, EnumNotificationType.AppointmentReminder);
        var sink = new FakeAuditSink();

        var sut = new RetryAdminNotificationCommandHandler(
            new FakeRepository<Notification>(new[] { notification }),
            sink,
            new FakeAuditContext());

        var result = await sut.Handle(new RetryAdminNotificationCommand
        {
            NotificationId = notification.Id,
            ActorUserId = AdminId,
            ActorRole = "PlatformAdmin",
            Reason = "Musteri bildirimi okumadigini bildirdi."
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(notification.Id, result.Value!.NotificationId);

        var record = Assert.Single(sink.Records);
        Assert.Equal(AdminId, record.ActorUserId);
        Assert.Equal("PlatformAdmin", record.ActorRole);
        Assert.Equal("Notification", record.EntityType);
        Assert.Equal(notification.Id.ToString(), record.EntityId);
        Assert.Equal("NotificationRetry", record.Action);
        Assert.Contains("IsRead", record.OldValues);
        Assert.Contains("false", record.OldValues);
        Assert.Equal("test-correlation-id", record.CorrelationId);
        Assert.Equal("127.0.0.1", record.IpAddress);

        // Basitlestirme notu istemciye acikca iletilir.
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Note));
    }

    // ===== Yardimcilar =====

    /// <summary>Bildirim uretir; domain event'leri test baglaminda temizlenir.</summary>
    private static Notification NewNotification(
        Guid userId,
        EnumNotificationType type,
        bool read = false,
        DateTime? sentAt = null)
    {
        var notification = new Notification(userId, "Test bildirimi", "Test mesaji", type);

        if (sentAt.HasValue)
            notification.SetSentAtForSeed(sentAt.Value);

        if (read)
            notification.MarkAsRead();

        notification.ClearDomainEvents();
        return notification;
    }

    /// <summary>Denetim kayitlarini bellekte toplayan sahte sink.</summary>
    private sealed class FakeAuditSink : IAdminAuditSink
    {
        public List<AuditRecord> Records { get; } = new();

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
            Records.Add(new AuditRecord(
                actorUserId, actorRole, businessId, entityType, entityId, action,
                oldValues, newValues, reason, correlationId, ipAddress));

            return Task.CompletedTask;
        }
    }

    /// <summary>Sabit denetim baglami; correlation ve IP dogrulanabilir.</summary>
    private sealed class FakeAuditContext : IAdminAuditContext
    {
        public string CorrelationId { get; } = "test-correlation-id";

        public string IpAddress { get; } = "127.0.0.1";
    }

    /// <summary>Sahte sink'e yazilan tek denetim kaydi.</summary>
    private sealed record AuditRecord(
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
}
