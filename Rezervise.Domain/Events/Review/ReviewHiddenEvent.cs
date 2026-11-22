namespace Rezervise.Domain.Events.Review
{
    public class ReviewHiddenEvent : ReviewEventBase
    {
        public string? HiddenBy { get; }
        public string? Reason { get; }

        public ReviewHiddenEvent(
            Guid reviewId,
            Guid userId,
            Guid businessId,
            int rating,
            string? comment,
            string? hiddenBy = null,
            string? reason = null) : base(reviewId, userId, businessId, rating, comment)
        {
            HiddenBy = hiddenBy;
            Reason = reason;
        }
    }
}