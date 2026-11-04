using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.Notification
{
    public class NotificationSentEvent : NotificationEventBase
    {
        public string? Channel { get; }
        public string? Recipient { get; }

        public NotificationSentEvent(
            Guid notificationId,
            Guid userId,
            EnumNotificationType type,
            string title,
            string? channel = null,
            string? recipient = null) : base(notificationId, userId, type, title)
        {
            Channel = channel;
            Recipient = recipient;
        }
    }
}