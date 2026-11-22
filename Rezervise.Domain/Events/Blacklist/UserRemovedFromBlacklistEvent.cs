namespace Rezervise.Domain.Events.Blacklist
{
    public class UserRemovedFromBlacklistEvent : DomainEventBase
    {
        public Guid BlacklistEntryId { get; }
        public Guid UserId { get; }
        public Guid BusinessId { get; }
        public string RemovedBy { get; }
        public string? Reason { get; }

        public UserRemovedFromBlacklistEvent(
            Guid blacklistEntryId,
            Guid userId,
            Guid businessId,
            string removedBy,
            string? reason) : base()
        {
            BlacklistEntryId = blacklistEntryId;
            UserId = userId;
            BusinessId = businessId;
            RemovedBy = removedBy;
            Reason = reason;
        }
    }
}
