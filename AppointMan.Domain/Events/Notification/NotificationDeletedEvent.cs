using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.Notification
{
    public class NotificationDeletedEvent : NotificationEventBase
    {
        public string? DeletedBy { get; }

        public NotificationDeletedEvent(
            Guid notificationId,
            Guid userId,
            EnumNotificationType type,
            string title,
            string? deletedBy = null) : base(notificationId, userId, type, title)
        {
            DeletedBy = deletedBy;
        }
    }
}