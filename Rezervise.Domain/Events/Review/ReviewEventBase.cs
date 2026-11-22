namespace Rezervise.Domain.Events.Review
{
    public abstract class ReviewEventBase : DomainEventBase
    {
        public Guid ReviewId { get; }
        public Guid UserId { get; }
        public Guid BusinessId { get; }
        public int Rating { get; }
        public string? Comment { get; }

        protected ReviewEventBase(
            Guid reviewId,
            Guid userId,
            Guid businessId,
            int rating,
            string? comment) : base()
        {
            ReviewId = reviewId;
            UserId = userId;
            BusinessId = businessId;
            Rating = rating;
            Comment = comment;
        }
    }
}