using AppointMan.Domain.Common;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using AppointMan.Domain.Events;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Il (ornek: Istanbul, Ankara).
    /// Id int cunku 81 il icin Guid gereksiz yer kaplar.
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
        /// Seed verisi icin ID'li kurucu.
        /// Turk plaka kodu (1-81) dogal anahtar olarak Id'ye atanir; boylece
        /// Address.CityId ile seed verisi arasinda kopus olmaz.
        /// </summary>
        public City(int id, string name, string countryCode)
        {
            if (id <= 0)
                throw new DomainException("Il ID gecersiz.");

            Id = id;
            SetName(name);
            SetCountryCode(countryCode);
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Il adi bos olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Il adi 2-100 karakter arasinda olmalidir.");

            Name = name.Trim();
            SearchName = SearchNormalizer.Normalize(Name);
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetCountryCode(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                throw new DomainException("Ulke kodu bos olamaz.");

            if (countryCode.Length != 2)
                throw new DomainException("Ulke kodu 2 karakter olmalidir (ISO 3166-1 alpha-2).");

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