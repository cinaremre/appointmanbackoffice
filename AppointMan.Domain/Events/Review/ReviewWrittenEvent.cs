namespace AppointMan.Domain.Events.Review
{
    public class ReviewWrittenEvent : ReviewEventBase
    {
        public Guid? AppointmentId { get; }
        public DateTime WrittenAt { get; }

        public ReviewWrittenEvent(
            Guid reviewId,
            Guid userId,
            Guid businessId,
            int rating,
            string? comment,
            Guid? appointmentId = null) : base(reviewId, userId, businessId, rating, comment)
        {
            AppointmentId = appointmentId;
            WrittenAt = DateTime.UtcNow;
        }
    }
}