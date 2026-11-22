namespace Rezervise.Domain.Events.Blacklist
{
    public class UserBlacklistedEvent : DomainEventBase
    {
        public Guid BlacklistEntryId { get; }
        public Guid UserId { get; }
        public Guid BusinessId { get; }
        public string? Reason { get; }

        public UserBlacklistedEvent(Guid blacklistEntryId, Guid userId, Guid businessId, string? reason) : base()
        {
            BlacklistEntryId = blacklistEntryId;
            UserId = userId;
            BusinessId = businessId;
            Reason = reason;
        }
    }
}
