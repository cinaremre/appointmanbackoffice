using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.Business
{
    public class BusinessClosedEvent : BusinessEventBase
    {
        public string? ClosedBy { get; }
        public string? Reason { get; }

        public BusinessClosedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType,
            string? closedBy = null,
            string? reason = null) : base(businessId, businessName, ownerId, businessType)
        {
            ClosedBy = closedBy;
            Reason = reason;
        }
    }
}