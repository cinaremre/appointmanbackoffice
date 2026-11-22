using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.Business
{
    public class BusinessOwnerChangedEvent : BusinessEventBase
    {
        public Guid OldOwnerId { get; }
        public Guid NewOwnerId { get; }
        public string? ChangedBy { get; }

        public BusinessOwnerChangedEvent(
            Guid businessId,
            string businessName,
            Guid oldOwnerId,
            Guid newOwnerId,
            EnumBusinessType businessType,
            string? changedBy = null) : base(businessId, businessName, newOwnerId, businessType)
        {
            OldOwnerId = oldOwnerId;
            NewOwnerId = newOwnerId;
            ChangedBy = changedBy;
        }
    }
}