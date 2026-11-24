using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.User
{
    public class UserRegisteredEvent : UserEventBase
    {
        public EnumUserRole Role { get; }
        public DateTime RegisteredAt { get; }

        public UserRegisteredEvent(
            Guid userId,
            string? email,
            string fullName,
            EnumUserRole role) : base(userId, email, fullName)
        {
            Role = role;
            RegisteredAt = DateTime.UtcNow;
        }
    }
}