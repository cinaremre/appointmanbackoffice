using System;
using AppointMan.Domain.Events.Review;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    public class Review : BaseEntity
    {
        public int Rating { get; private set; }
        public string Comment { get; private set; }
        public DateTime ReviewDate { get; private set; }
        public bool IsVisible { get; private set; }

        // Foreign Keys
        public Guid UserId { get; private set; }
        public Guid BusinessId { get; private set; }
        public Guid? AppointmentId { get; private set; }

        // Navigation Properties
        public User User { get; private set; }
        public Business Business { get; private set; }
        public Appointment Appointment { get; private set; }

        private Review() { } // EF Core

        public Review(Guid userId, Guid businessId, int rating, string comment, Guid? appointmentId = null)
    : this(Guid.NewGuid(), userId, businessId, rating, comment, appointmentId)
        {
        }
        public Review(Guid id, Guid userId, Guid businessId, int rating, string comment, Guid? appointmentId = null)
    : base(id)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Kullanıcı ID boş olamaz.", nameof(userId));

            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            UserId = userId;
            BusinessId = businessId;
            AppointmentId = appointmentId;

            SetRating(rating);
            SetComment(comment);

            ReviewDate = DateTime.UtcNow;
            IsVisible = true;

            AddDomainEvent(new ReviewWrittenEvent(Id, UserId, BusinessId, Rating, Comment, AppointmentId));
        }
        public void SetRating(int rating)
        {
            if (rating < 1 || rating > 5)
                throw new DomainException("Puan 1-5 arasında olmalıdır.");

            var oldRating = Rating;
            Rating = rating;
            UpdatedAt = DateTime.UtcNow;

            if (oldRating != rating && oldRating > 0)
            {
                AddDomainEvent(new ReviewUpdatedEvent(
                    Id, UserId, BusinessId,
                    oldRating, rating,
                    Comment, Comment,
                    "System"));
            }
        }

        public void SetComment(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                throw new DomainException("Yorum boş olamaz.");

            if (comment.Length < 10 || comment.Length > 500)
                throw new DomainException("Yorum 10-500 karakter arasında olmalıdır.");

            var oldComment = Comment;
            Comment = comment;
            UpdatedAt = DateTime.UtcNow;

            if (oldComment != comment && !string.IsNullOrEmpty(oldComment))
            {
                AddDomainEvent(new ReviewUpdatedEvent(
                    Id, UserId, BusinessId,
                    Rating, Rating,
                    oldComment, comment,
                    "System"));
            }
        }

        public void Hide(string? hiddenBy = null, string? reason = null)
        {
            if (!IsVisible)
                return;

            IsVisible = false;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new ReviewHiddenEvent(Id, UserId, BusinessId, Rating, Comment, hiddenBy, reason));
        }

        public void Show()
        {
            if (IsVisible)
                return;

            IsVisible = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateReview(int rating, string comment, string? updatedBy = null)
        {
            var oldRating = Rating;
            var oldComment = Comment;

            SetRating(rating);
            SetComment(comment);

            if (oldRating != rating || oldComment != comment)
            {
                AddDomainEvent(new ReviewUpdatedEvent(
                    Id, UserId, BusinessId,
                    oldRating, rating,
                    oldComment, comment,
                    updatedBy ?? "User"));
            }
        }

        public void Delete(string? deletedBy = null, string? reason = null)
        {
            if (IsDeleted)
                return;

            SoftDelete();
            AddDomainEvent(new ReviewDeletedEvent(Id, UserId, BusinessId, Rating, Comment, deletedBy, reason));
        }

        public void Report(Guid reportedBy, string? reportReason = null)
        {
            AddDomainEvent(new ReviewReportedEvent(Id, UserId, BusinessId, Rating, Comment, reportedBy, reportReason));
        }
        public void SetReviewDateForSeed(DateTime reviewDate)
        {
            ReviewDate = reviewDate;
        }
    }
}