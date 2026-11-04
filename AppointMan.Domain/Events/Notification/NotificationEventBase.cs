using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.Notification
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