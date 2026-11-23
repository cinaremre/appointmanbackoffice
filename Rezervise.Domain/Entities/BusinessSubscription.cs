using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İşletmenin bir abonelik planına bağlanması (doküman Modül 14).
    /// Aynı anda tek aktif abonelik beklenir; yeni atamada eskisi Expired yapılır.
    /// </summary>
    public class BusinessSubscription : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid PlanId { get; private set; }
        public DateTime StartAt { get; private set; }

        /// <summary>UTC bitiş anı; null = belirsiz süre.</summary>
        public DateTime? EndAt { get; private set; }

        public EnumSubscriptionStatus Status { get; private set; }

        // Navigation Properties
        public Business Business { get; private set; }
        public SubscriptionPlan Plan { get; private set; }

        private BusinessSubscription() { } // EF Core

        public BusinessSubscription(Guid businessId, Guid planId, DateTime startAt, DateTime? endAt = null)
            : this(Guid.NewGuid(), businessId, planId, startAt, endAt)
        {
        }

        public BusinessSubscription(Guid id, Guid businessId, Guid planId, DateTime startAt, DateTime? endAt = null)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (planId == Guid.Empty)
                throw new ArgumentException("Plan ID boş olamaz.", nameof(planId));

            if (endAt.HasValue && endAt.Value <= startAt)
                throw new DomainException("Abonelik bitiş tarihi başlangıçtan sonra olmalı.");

            BusinessId = businessId;
            PlanId = planId;
            StartAt = DateTime.SpecifyKind(startAt, DateTimeKind.Utc);
            EndAt = endAt.HasValue
                ? DateTime.SpecifyKind(endAt.Value, DateTimeKind.Utc)
                : null;
            Status = EnumSubscriptionStatus.Active;
        }

        /// <summary>
        /// Verilen UTC anda abonelik aktif mi? Durum Active olmalı ve
        /// now [StartAt, EndAt) aralığına düşmeli.
        /// </summary>
        public bool IsActiveAt(DateTime utc)
        {
            return Status == EnumSubscriptionStatus.Active
                && StartAt <= utc
                && (EndAt is null || utc < EndAt.Value)
                && !IsDeleted;
        }

        /// <summary>Yeni atama veya süre dolusu; sadece aktif abonelik expire edilir.</summary>
        public void MarkExpired()
        {
            if (Status != EnumSubscriptionStatus.Active)
                return;

            Status = EnumSubscriptionStatus.Expired;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkCancelled()
        {
            if (Status != EnumSubscriptionStatus.Active)
                return;

            Status = EnumSubscriptionStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
