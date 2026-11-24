using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.User
{
    public class UserPasswordChangedEvent : UserEventBase
    {
        public string? ChangedBy { get; }
        public DateTime ChangedAt { get; }

        public UserPasswordChangedEvent(
            Guid userId,
            string? email,
            string fullName,
            string? changedBy = null) : base(userId, email, fullName)
        {
            ChangedBy = changedBy;
            ChangedAt = DateTime.UtcNow;
        }
    }
}