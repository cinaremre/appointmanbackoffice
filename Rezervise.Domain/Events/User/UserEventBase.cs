using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.User
{
    public abstract class UserEventBase : DomainEventBase
    {
        public Guid UserId { get; }
        public string? Email { get; }
        public string FullName { get; }

        protected UserEventBase(
            Guid userId,
            string? email,
            string fullName) : base()
        {
            UserId = userId;
            Email = email;
            FullName = fullName;
        }
    }
}