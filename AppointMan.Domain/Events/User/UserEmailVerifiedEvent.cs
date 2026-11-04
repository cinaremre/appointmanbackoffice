using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.User
{
    public class UserEmailVerifiedEvent : UserEventBase
    {
        public DateTime VerifiedAt { get; }

        public UserEmailVerifiedEvent(
            Guid userId,
            string email,
            string fullName) : base(userId, email, fullName)
        {
            VerifiedAt = DateTime.UtcNow;
        }
    }
}