using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.Business
{
    public class BusinessReviewReceivedEvent : BusinessEventBase
    {
        public Guid ReviewId { get; }
        public int Rating { get; }
        public string? Comment { get; }
        public Guid UserId { get; }

        public BusinessReviewReceivedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType,
            Guid reviewId,
            int rating,
            string? comment,
            Guid userId) : base(businessId, businessName, ownerId, businessType)
        {
            ReviewId = reviewId;
            Rating = rating;
            Comment = comment;
            UserId = userId;
        }
    }
}