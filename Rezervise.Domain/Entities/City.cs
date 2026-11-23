using Rezervise.Domain.Common;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.Events;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İl (örnek: Istanbul, Ankara).
    /// Id int çünkü 81 il için Guid gereksiz yer kaplar.
    /// </summary>
    public class City : IAggregateRoot
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string SearchName { get; private set; }
        public string CountryCode { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Navigation
        private readonly List<District> _districts = new();
        public IReadOnlyCollection<District> Districts => _districts.AsReadOnly();

        // IAggregateRoot
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();
        public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

        private City() { } // EF Core

        public City(string name, string countryCode)
        {
            SetName(name);
            SetCountryCode(countryCode);
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Seed verişi için ID'li kurucu.
        /// Türk plaka kodu (1-81) doğal anahtar olarak Id'ye atanır; böylece
        /// Address.CityId ile seed verişi arasında kopuş olmaz.
        /// </summary>
        public City(int id, string name, string countryCode)
        {
            if (id <= 0)
                throw new DomainException("City.IdInvalid");

            Id = id;
            SetName(name);
            SetCountryCode(countryCode);
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("City.NameRequired");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("City.NameLength");

            Name = name.Trim();
            SearchName = SearchNormalizer.Normalize(Name);
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetCountryCode(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                throw new DomainException("City.CountryCodeRequired");

            if (countryCode.Length != 2)
                throw new DomainException("City.CountryCodeLength");

            CountryCode = countryCode.ToUpperInvariant();
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