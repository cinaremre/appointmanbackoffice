using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Isletmenin sattigi seans paketi tanimi (dokuman Modul 5).
    /// Paketin icindeki hizmetler <see cref="ServicePackageItem"/> cocuk
    /// entity'leriyle tutulur; aggregate root budur.
    /// </summary>
    public class ServicePackage : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public string Name { get; private set; }
        public decimal Price { get; private set; }
        public string Currency { get; private set; }
        public int ValidityDays { get; private set; }
        public bool IsActive { get; private set; }

        // Navigation
        private readonly List<ServicePackageItem> _items = new();
        public IReadOnlyCollection<ServicePackageItem> Items => _items.AsReadOnly();

        private ServicePackage() { } // EF Core

        public ServicePackage(string name, decimal price, int validityDays, Guid businessId)
            : this(Guid.NewGuid(), name, price, validityDays, businessId)
        {
        }

        public ServicePackage(Guid id, string name, decimal price, int validityDays, Guid businessId)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            SetName(name);
            SetPrice(price);
            SetValidityDays(validityDays);
            SetCurrency("TRY");
            BusinessId = businessId;
            IsActive = true;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("ServicePackage.NameRequired");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("ServicePackage.NameLength");

            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPrice(decimal price)
        {
            if (price < 0)
                throw new DomainException("ServicePackage.PriceNegative");

            if (price > 99999999.99m)
                throw new DomainException("ServicePackage.PriceTooHigh");

            Price = Math.Round(price, 2);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Para birimi. Ilk surunde yalnizca TRY desteklenir.</summary>
        public void SetCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new DomainException("ServicePackage.CurrencyRequired");

            if (currency.Length != 3)
                throw new DomainException("ServicePackage.CurrencyLength");

            Currency = currency.ToUpperInvariant();
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetValidityDays(int validityDays)
        {
            if (validityDays < 1 || validityDays > 3650)
                throw new DomainException("ServicePackage.ValidityDaysRange");

            ValidityDays = validityDays;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Paketi satisa acar. Zaten aktifse hicbir sey yapmaz.</summary>
        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Paketi satisa kapatir. Zaten pasifse hicbir sey yapmaz.</summary>
        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Pakete yeni bir hizmet kalemi ekler. Ayni hizmet iki kez eklenemez.</summary>
        public void AddItem(ServicePackageItem item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            if (item.ServicePackageId != Id)
                throw new DomainException("ServicePackage.ItemBelongsToPackage");

            if (_items.Any(i => i.ServiceId == item.ServiceId && !i.IsDeleted))
                throw new DomainException("ServicePackage.DuplicateService");

            _items.Add(item);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Pakette satin alinan toplam seans sayisi. Kalan hak hesabinin
        /// "purchased toplam" bileseni; hareket toplamlarindan cikarilir.
        /// </summary>
        public int GetTotalSessions()
        {
            return _items.Where(i => !i.IsDeleted).Sum(i => i.IncludedSessions);
        }
    }
}
