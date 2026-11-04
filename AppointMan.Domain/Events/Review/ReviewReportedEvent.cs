namespace AppointMan.Domain.Events.Review
{
    public class ReviewReportedEvent : ReviewEventBase
    {
        public Guid ReportedBy { get; }
        public string? ReportReason { get; }

        public ReviewReportedEvent(
            Guid reviewId,
            Guid userId,
            Guid businessId,
            int rating,
            string? comment,
            Guid reportedBy,
            string? reportReason = null) : base(reviewId, userId, businessId, rating, comment)
        {
            ReportedBy = reportedBy;
            ReportReason = reportReason;
        }
    }
}