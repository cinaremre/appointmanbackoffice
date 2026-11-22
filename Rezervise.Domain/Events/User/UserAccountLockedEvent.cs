using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.User
{
    public class UserAccountLockedEvent : UserEventBase
    {
        public string? Reason { get; }
        public DateTime LockedAt { get; }
        public string? LockedBy { get; }

        public UserAccountLockedEvent(
            Guid userId,
            string email,
            string fullName,
            string? reason = null,
            string? lockedBy = null) : base(userId, email, fullName)
        {
            Reason = reason;
            LockedBy = lockedBy;
            LockedAt = DateTime.UtcNow;
        }
    }
}