using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.Business
{
    public class BusinessActivatedEvent : BusinessEventBase
    {
        public string? ActivatedBy { get; }

        public BusinessActivatedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType,
            string? activatedBy = null) : base(businessId, businessName, ownerId, businessType)
        {
            ActivatedBy = activatedBy;
        }
    }
}