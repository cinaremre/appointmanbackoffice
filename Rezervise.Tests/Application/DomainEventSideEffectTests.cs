using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Application.Common.Services;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Events.Appointment;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

public sealed class DomainEventSideEffectTests
{
    [Fact]
    public void Deduplicator_AllowsEachEventOnlyOnce()
    {
        var deduplicator = new EventSideEffectDeduplicator();
        var eventId = Guid.NewGuid();

        Assert.True(deduplicator.TryRegister(eventId));
        Assert.False(deduplicator.TryRegister(eventId));
        Assert.True(deduplicator.TryRegister(Guid.NewGuid()));
        Assert.False(deduplicator.TryRegister(Guid.Empty));
    }

    [Fact]
    public async Task AppointmentEvent_EnqueuesEmailAndAuditOnce()
    {
        var user = TestData.NewUser(Guid.NewGuid());
        var users = new FakeRepository<User>([user]);
        var queue = new RecordingEmailQueue();
        var audit = new RecordingAuditSink();

        var handler = new AppointmentEventSideEffectHandler(
            queue,
            new RecordingPushQueue(),
            users,
            new FakeRepository<DeviceToken>(),
            audit,
            new EventSideEffectDeduplicator());

        var appointmentEvent = new AppointmentCreatedEvent(
            Guid.NewGuid(),
            user.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1));

        var notification = new DomainEventNotification(appointmentEvent);

        await handler.Handle(notification, CancellationToken.None);
        await handler.Handle(notification, CancellationToken.None);

        Assert.Single(queue.Messages);
        Assert.Equal(user.Email, queue.Messages[0].To);
        Assert.Single(audit.Records);
        Assert.Equal(nameof(AppointmentCreatedEvent), audit.Records[0].EventType);
    }

    [Fact]
    public async Task AppointmentEvent_EnqueuesPushForActiveDeviceTokens()
    {
        var user = TestData.NewUser(Guid.NewGuid());
        var users = new FakeRepository<User>([user]);

        var activeToken = new DeviceToken(user.Id, new string('a', 64), EnumDevicePlatform.Android, DateTime.UtcNow);
        var inactiveToken = new DeviceToken(user.Id, new string('b', 64), EnumDevicePlatform.Ios, DateTime.UtcNow);
        inactiveToken.Deactivate();

        var deviceTokens = new FakeRepository<DeviceToken>([activeToken, inactiveToken]);
        var pushQueue = new RecordingPushQueue();

        var handler = new AppointmentEventSideEffectHandler(
            new RecordingEmailQueue(),
            pushQueue,
            users,
            deviceTokens,
            new RecordingAuditSink(),
            new EventSideEffectDeduplicator());

        var appointmentEvent = new AppointmentConfirmedEvent(
            Guid.NewGuid(),
            user.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(2));

        await handler.Handle(new DomainEventNotification(appointmentEvent), CancellationToken.None);

        // Yalnizca aktif cihaza push kuyruga alinir; pasif token atlanir.
        var message = Assert.Single(pushQueue.Messages);
        Assert.Equal(activeToken.Token, message.Token);
        Assert.Equal(user.Id, message.UserId);
        Assert.Equal("Randevunuz onaylandı", message.Title);
        Assert.NotEqual(string.Empty, message.Body);
    }

    private sealed class RecordingEmailQueue : IEmailQueue
    {
        public List<EmailMessage> Messages { get; } = [];

        public bool TryEnqueue(EmailMessage message)
        {
            Messages.Add(message);
            return true;
        }
    }

    private sealed class RecordingPushQueue : IPushQueue
    {
        public List<PushMessage> Messages { get; } = [];

        public bool TryEnqueue(PushMessage message)
        {
            Messages.Add(message);
            return true;
        }
    }

    private sealed class RecordingAuditSink : IWorkflowAuditSink
    {
        public List<(string EventType, Guid EventId, Guid? AggregateId, string? Details)> Records { get; } = [];

        public void Record(string eventType, Guid eventId, Guid? aggregateId, string? details)
        {
            Records.Add((eventType, eventId, aggregateId, details));
        }
    }
}
