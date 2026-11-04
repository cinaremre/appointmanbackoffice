using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.User
{
    public class UserPhoneVerifiedEvent : UserEventBase
    {
        public string PhoneNumber { get; }
        public DateTime VerifiedAt { get; }

        public UserPhoneVerifiedEvent(
            Guid userId,
            string email,
            string fullName,
            string phoneNumber) : base(userId, email, fullName)
        {
            PhoneNumber = phoneNumber;
            VerifiedAt = DateTime.UtcNow;
        }
    }
}