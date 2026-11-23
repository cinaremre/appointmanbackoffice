using Rezervise.Domain.Events.Blacklist;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    public class BlacklistEntry : BaseEntity
    {
        public Guid UserId { get; private set; }
        public User User { get; private set; }

        public Guid BusinessId { get; private set; }
        public Business Business { get; private set; }

        public string? Reason { get; private set; }
        public int AppointmentCount { get; private set; }
        public DateTime BlacklistedAt { get; private set; }

        public DateTime? RemovedAt { get; private set; }
        public string? RemovedBy { get; private set; }
        public string? RemoveReason { get; private set; }

        private BlacklistEntry() { } // EF Core

        public BlacklistEntry(Guid userId, Guid businessId, int appointmentCount, string? reason = null)
            : this(Guid.NewGuid(), userId, businessId, appointmentCount, reason)
        {
        }

        public BlacklistEntry(Guid id, Guid userId, Guid businessId, int appointmentCount, string? reason = null)
            : base(id)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Kullanıcı ID boş olamaz.", nameof(userId));

            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            UserId = userId;
            BusinessId = businessId;
            AppointmentCount = appointmentCount;
            Reason = reason;
            BlacklistedAt = DateTime.UtcNow;

            AddDomainEvent(new UserBlacklistedEvent(Id, UserId, BusinessId, reason));
        }

        public void Remove(string removedBy, string? reason = null)
        {
            if (IsDeleted)
                throw new DomainException("Bu kayıt zaten kara listeden çıkarılmış.");

            if (string.IsNullOrWhiteSpace(removedBy))
                throw new DomainException("Çıkaran kişi boş olamaz.");

            RemovedAt = DateTime.UtcNow;
            RemovedBy = removedBy;
            RemoveReason = reason;
            SoftDelete();

            AddDomainEvent(new UserRemovedFromBlacklistEvent(Id, UserId, BusinessId, removedBy, reason));
        }
    }
}