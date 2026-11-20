using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Models;
using AppointMan.Application.Common.Services;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Events.Appointment;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

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
            users,
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

    private sealed class RecordingEmailQueue : IEmailQueue
    {
        public List<EmailMessage> Messages { get; } = [];

        public bool TryEnqueue(EmailMessage message)
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
