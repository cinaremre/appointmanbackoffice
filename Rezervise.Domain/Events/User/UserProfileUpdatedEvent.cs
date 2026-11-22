using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.User
{
    public class UserProfileUpdatedEvent : UserEventBase
    {
        public string? UpdatedBy { get; }
        public Dictionary<string, object> Changes { get; }

        public UserProfileUpdatedEvent(
            Guid userId,
            string email,
            string fullName,
            string? updatedBy = null) : base(userId, email, fullName)
        {
            UpdatedBy = updatedBy;
            Changes = new Dictionary<string, object>();
        }

        public void AddChange(string key, object oldValue, object newValue)
        {
            Changes[key] = new { Old = oldValue, New = newValue };
        }
    }
}