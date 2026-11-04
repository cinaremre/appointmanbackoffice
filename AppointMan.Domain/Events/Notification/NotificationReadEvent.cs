using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.Notification
{
    public class NotificationReadEvent : NotificationEventBase
    {
        public DateTime ReadAt { get; }

        public NotificationReadEvent(
            Guid notificationId,
            Guid userId,
            EnumNotificationType type,
            string title) : base(notificationId, userId, type, title)
        {
            ReadAt = DateTime.UtcNow;
        }
    }
}