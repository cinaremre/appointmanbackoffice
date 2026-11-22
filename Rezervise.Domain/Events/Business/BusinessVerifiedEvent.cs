using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.Business
{
    public class BusinessVerifiedEvent : BusinessEventBase
    {
        public string? VerifiedBy { get; }
        public DateTime VerifiedAt { get; }

        public BusinessVerifiedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType,
            string? verifiedBy = null) : base(businessId, businessName, ownerId, businessType)
        {
            VerifiedBy = verifiedBy;
            VerifiedAt = DateTime.UtcNow;
        }
    }
}