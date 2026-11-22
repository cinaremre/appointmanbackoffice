using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.Notification
{
    public abstract class NotificationEventBase : DomainEventBase
    {
        public Guid NotificationId { get; }
        public Guid UserId { get; }
        public EnumNotificationType Type { get; }
        public string Title { get; }

        protected NotificationEventBase(
            Guid notificationId,
            Guid userId,
            EnumNotificationType type,
            string title) : base()
        {
            NotificationId = notificationId;
            UserId = userId;
            Type = type;
            Title = title;
        }
    }
}