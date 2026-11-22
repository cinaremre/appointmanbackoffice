using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.Business
{
    public class BusinessSuspendedEvent : BusinessEventBase
    {
        public string? SuspendedBy { get; }
        public string? Reason { get; }
        public DateTime? SuspendedUntil { get; }

        public BusinessSuspendedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType,
            string? suspendedBy = null,
            string? reason = null,
            DateTime? suspendedUntil = null) : base(businessId, businessName, ownerId, businessType)
        {
            SuspendedBy = suspendedBy;
            Reason = reason;
            SuspendedUntil = suspendedUntil;
        }
    }
}