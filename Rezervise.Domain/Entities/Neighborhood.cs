using Rezervise.Domain.Common;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.Events;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Mahalle (örnek: Caferağa, Moda).
    /// Her mahallenin bir locality'si vardır.
    /// Zincir: İl -> İlçe -> Locality -> Mahalle.
    /// </summary>
    public class Neighborhood : IAggregateRoot
    {
        public int Id { get; private set; }
        public int LocalityId { get; private set; }
        public Locality Locality { get; private set; }
        public string Name { get; private set; }
        public string SearchName { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // IAggregateRoot
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();
        public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

        private Neighborhood() { } // EF Core

        public Neighborhood(int localityId, string name)
        {
            if (localityId <= 0)
                throw new DomainException("Locality.IdInvalid");

            LocalityId = localityId;
            SetName(name);
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Seed verişi için ID'li kurucu.
        /// Kaynak verideki mahalle ID'si korunur; referans veri aktarımında
        /// ID'lerin değişmemesi gerekir.
        /// </summary>
        public Neighborhood(int id, int localityId, string name)
        {
            if (id <= 0)
                throw new DomainException("Neighborhood.IdInvalid");

            if (localityId <= 0)
                throw new DomainException("Locality.IdInvalid");

            Id = id;
            LocalityId = localityId;
            SetName(name);
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Neighborhood.NameRequired");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Neighborhood.NameLength");

            Name = name.Trim();
            SearchName = SearchNormalizer.Normalize(Name);
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
