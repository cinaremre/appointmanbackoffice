namespace Rezervise.Domain.Events.Review
{
    public class ReviewUpdatedEvent : ReviewEventBase
    {
        public int OldRating { get; }
        public string? OldComment { get; }
        public string? UpdatedBy { get; }

        public ReviewUpdatedEvent(
            Guid reviewId,
            Guid userId,
            Guid businessId,
            int oldRating,
            int newRating,
            string? oldComment,
            string? newComment,
            string? updatedBy = null) : base(reviewId, userId, businessId, newRating, newComment)
        {
            OldRating = oldRating;
            OldComment = oldComment;
            UpdatedBy = updatedBy;
        }
    }
}