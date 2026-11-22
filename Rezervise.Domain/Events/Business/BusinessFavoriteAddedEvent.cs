using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.Business
{
    public class BusinessFavoriteAddedEvent : BusinessEventBase
    {
        public Guid UserId { get; }

        public BusinessFavoriteAddedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType,
            Guid userId) : base(businessId, businessName, ownerId, businessType)
        {
            UserId = userId;
        }
    }
}