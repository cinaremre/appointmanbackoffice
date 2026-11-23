using System;
using Rezervise.Domain.Events.Favorite;

namespace Rezervise.Domain.Entities
{
    public class Favorite : BaseEntity
    {
        public Guid UserId { get; private set; }
        public Guid BusinessId { get; private set; }
        public DateTime AddedAt { get; private set; }
        public string? Note { get; private set; }

        // Navigation Properties
        public User User { get; private set; }
        public Business Business { get; private set; }

        private Favorite() { } // EF Core

        public Favorite(Guid userId, Guid businessId, string? note = null)
    : this(Guid.NewGuid(), userId, businessId, note)
        {
        }
        public Favorite(Guid id, Guid userId, Guid businessId, string? note = null)
    : base(id)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Kullanıcı ID boş olamaz.", nameof(userId));

            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            UserId = userId;
            BusinessId = businessId;
            Note = note;
            AddedAt = DateTime.UtcNow;

            AddDomainEvent(new FavoriteAddedEvent(Id, UserId, BusinessId, Note));
        }

        public void UpdateNote(string? note)
        {
            Note = note;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Delete(string? deletedBy = null)
        {
            if (IsDeleted)
                return;

            SoftDelete();
            AddDomainEvent(new FavoriteRemovedEvent(Id, UserId, BusinessId, deletedBy));
        }
        public void SetAddedAtForSeed(DateTime addedAt)
        {
            AddedAt = addedAt;
        }
    }
}