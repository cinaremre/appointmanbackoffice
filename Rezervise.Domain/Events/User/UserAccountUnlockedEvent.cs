using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.User
{
    public class UserAccountUnlockedEvent : UserEventBase
    {
        public string? UnlockedBy { get; }
        public DateTime UnlockedAt { get; }

        public UserAccountUnlockedEvent(
            Guid userId,
            string email,
            string fullName,
            string? unlockedBy = null) : base(userId, email, fullName)
        {
            UnlockedBy = unlockedBy;
            UnlockedAt = DateTime.UtcNow;
        }
    }
}