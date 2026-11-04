using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.Business
{
    public class BusinessDeactivatedEvent : BusinessEventBase
    {
        public string? DeactivatedBy { get; }
        public string? Reason { get; }

        public BusinessDeactivatedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType,
            string? deactivatedBy = null,
            string? reason = null) : base(businessId, businessName, ownerId, businessType)
        {
            DeactivatedBy = deactivatedBy;
            Reason = reason;
        }
    }
}