using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.Notification
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