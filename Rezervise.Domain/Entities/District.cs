using Rezervise.Domain.Common;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.Events;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İlçe (örnek: Kadıkoy, Beşiktaş).
    /// Her ilçenin bir ili vardır.
    /// </summary>
    public class District : IAggregateRoot
    {
        public int Id { get; private set; }
        public int CityId { get; private set; }
        public City City { get; private set; }
        public string Name { get; private set; }
        public string SearchName { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Navigation
        private readonly List<Locality> _localities = new();
        public IReadOnlyCollection<Locality> Localities => _localities.AsReadOnly();

        // IAggregateRoot
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();
        public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

        private District() { } // EF Core

        public District(int cityId, string name)
        {
            if (cityId <= 0)
                throw new DomainException("City.IdInvalid");

            CityId = cityId;
            SetName(name);
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Seed verişi için ID'li kurucu.
        /// Kaynak verideki ilçe ID'si korunur; semt seed'i bu ID'ye bağlanacağı
        /// için ID'nin değişmemesi kritiktir.
        /// </summary>
        public District(int id, int cityId, string name)
        {
            if (id <= 0)
                throw new DomainException("District.IdInvalid");

            if (cityId <= 0)
                throw new DomainException("City.IdInvalid");

            Id = id;
            CityId = cityId;
            SetName(name);
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("District.NameRequired");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("District.NameLength");

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