using AppointMan.Domain.Events;
using AppointMan.Domain.Interfaces;

namespace AppointMan.Domain.Entities
{
    public abstract class BaseEntity : IAggregateRoot
    {
        public Guid Id { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime? UpdatedAt { get; protected set; }
        public bool IsDeleted { get; protected set; }
        public DateTime? DeletedAt { get; protected set; }

        /// <summary>
        /// EF Core optimistic concurrency token.
        /// SQL Server tarafindan her UPDATE'te otomatik uretilir; es zamanli
        /// iki guncellemeden ikincisi DbUpdateConcurrencyException alir ve
        /// sessiz veri kaybi (lost update) onlenir.
        /// </summary>
        public byte[]? RowVersion { get; protected set; }

        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected BaseEntity(Guid id)
        {
            Id = id;
            CreatedAt = DateTime.UtcNow;
            IsDeleted = false;
        }

        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            IsDeleted = false;
        }

        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}