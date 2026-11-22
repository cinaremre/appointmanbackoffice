using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.Business
{
    public abstract class BusinessEventBase : DomainEventBase
    {
        public Guid BusinessId { get; }
        public string BusinessName { get; }
        public Guid OwnerId { get; }
        public EnumBusinessType BusinessType { get; }

        protected BusinessEventBase(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType) : base()
        {
            BusinessId = businessId;
            BusinessName = businessName;
            OwnerId = ownerId;
            BusinessType = businessType;
        }
    }
}