using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.Business
{
    public class BusinessRatingUpdatedEvent : BusinessEventBase
    {
        public decimal OldRating { get; }
        public decimal NewRating { get; }

        public BusinessRatingUpdatedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType,
            decimal oldRating,
            decimal newRating) : base(businessId, businessName, ownerId, businessType)
        {
            OldRating = oldRating;
            NewRating = newRating;
        }
    }
}