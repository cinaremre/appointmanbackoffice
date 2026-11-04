namespace AppointMan.Domain.Events.Review
{
    public class ReviewDeletedEvent : ReviewEventBase
    {
        public string? DeletedBy { get; }
        public string? Reason { get; }

        public ReviewDeletedEvent(
            Guid reviewId,
            Guid userId,
            Guid businessId,
            int rating,
            string? comment,
            string? deletedBy = null,
            string? reason = null) : base(reviewId, userId, businessId, rating, comment)
        {
            DeletedBy = deletedBy;
            Reason = reason;
        }
    }
}