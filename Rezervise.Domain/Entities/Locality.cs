using Rezervise.Domain.Common;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.Events;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Semt / Locality (ornek: Caferaga, Yagcami).
    /// Her locality'nin bir ilcesi, her mahallenin bir locality'si vardir.
    /// Zincir: Il -> Ilce -> Locality -> Mahalle.
    /// </summary>
    public class Locality : IAggregateRoot
    {
        public int Id { get; private set; }
        public int DistrictId { get; private set; }
        public District District { get; private set; }
        public string Name { get; private set; }
        public string SearchName { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Navigation
        private readonly List<Neighborhood> _neighborhoods = new();
        public IReadOnlyCollection<Neighborhood> Neighborhoods => _neighborhoods.AsReadOnly();

        // IAggregateRoot
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();
        public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

        private Locality() { } // EF Core

        public Locality(int districtId, string name)
        {
            if (districtId <= 0)
                throw new DomainException("District.IdInvalid");

            DistrictId = districtId;
            SetName(name);
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Seed verisi icin ID'li kurucu.
        /// Kaynak verideki locality ID'si korunur; mahalle seed'i bu ID'ye
        /// baglanacagi icin ID'nin degismemesi kritiktir.
        /// </summary>
        public Locality(int id, int districtId, string name)
        {
            if (id <= 0)
                throw new DomainException("Locality.IdInvalid");

            if (districtId <= 0)
                throw new DomainException("District.IdInvalid");

            Id = id;
            DistrictId = districtId;
            SetName(name);
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Locality.NameRequired");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Locality.NameLength");

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
