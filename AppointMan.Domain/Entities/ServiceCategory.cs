using AppointMan.Domain.Events.ServiceCategory;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    public class ServiceCategory : BaseEntity
    {
        public string? IconUrl { get; private set; }
        public string? ColorHex { get; private set; }
        public int DisplayOrder { get; private set; }
        public bool IsActive { get; private set; }

        public Guid? ParentCategoryId { get; private set; }
        public ServiceCategory? ParentCategory { get; private set; }

        private readonly List<ServiceCategory> _subCategories = new();
        public IReadOnlyCollection<ServiceCategory> SubCategories => _subCategories.AsReadOnly();

        private readonly List<Service> _services = new();
        public IReadOnlyCollection<Service> Services => _services.AsReadOnly();

        private readonly List<ServiceCategoryTranslation> _translations = new();
        public IReadOnlyCollection<ServiceCategoryTranslation> Translations => _translations.AsReadOnly();

        private ServiceCategory() { }

        public ServiceCategory(int displayOrder = 0)
    : this(Guid.NewGuid(), displayOrder)
        {
        }
        public ServiceCategory(Guid id, int displayOrder = 0)
    : base(id)
        {
            SetDisplayOrder(displayOrder);
            IsActive = true;

            AddDomainEvent(new ServiceCategoryCreatedEvent(Id, displayOrder));
        }
        public void SetIconUrl(string? iconUrl)
        {
            if (iconUrl != null && !Uri.IsWellFormedUriString(iconUrl, UriKind.Absolute))
                throw new DomainException("Gecersiz ikon URL formati.");

            IconUrl = iconUrl;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetColorHex(string? colorHex)
        {
            if (colorHex != null)
            {
                if (!colorHex.StartsWith("#") || (colorHex.Length != 7 && colorHex.Length != 4))
                    throw new DomainException("Gecersiz renk formati. Ornek: #FF5722 veya #F52");
            }

            ColorHex = colorHex;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetDisplayOrder(int order)
        {
            if (order < 0)
                throw new DomainException("Siralama numarasi negatif olamaz.");

            DisplayOrder = order;
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

        public void SetParentCategory(ServiceCategory? parentCategory)
        {
            if (parentCategory != null && parentCategory.Id == Id)
                throw new DomainException("Kategori kendisinin ust kategorisi olamaz.");

            if (parentCategory != null && parentCategory.ParentCategoryId == Id)
                throw new DomainException("Dongusel kategori iliskisi olusturulamaz.");

            if (parentCategory != null && _subCategories.Any())
                throw new DomainException("Alt kategorisi olan bir kategori baska bir kategoriye baglanamaz.");

            if (parentCategory != null && _services.Any())
                throw new DomainException("Hizmeti olan bir kategori baska bir kategoriye baglanamaz.");

            if (ParentCategory != null)
            {
                ParentCategory._subCategories.Remove(this);
            }

            ParentCategory = parentCategory;
            ParentCategoryId = parentCategory?.Id;

            if (parentCategory != null && !parentCategory._subCategories.Contains(this))
            {
                parentCategory._subCategories.Add(this);
            }

            UpdatedAt = DateTime.UtcNow;
        }

        public void AddTranslation(string languageCode, string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                throw new DomainException("Dil kodu bos olamaz.");

            if (_translations.Any(t => t.LanguageCode.Equals(languageCode.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"'{languageCode}' dili icin zaten ceviri mevcut.");

            var translation = new ServiceCategoryTranslation(Id, languageCode, name, description);
            _translations.Add(translation);
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new ServiceCategoryTranslationAddedEvent(Id, translation.LanguageCode, name));
        }

        public void UpdateTranslation(string languageCode, string name, string? description = null)
        {
            var normalized = languageCode.Trim().ToLowerInvariant();
            var translation = _translations.FirstOrDefault(t =>
                t.LanguageCode.Equals(normalized, StringComparison.OrdinalIgnoreCase));

            if (translation is null)
                throw new DomainException($"'{languageCode}' dili icin ceviri bulunamadi.");

            translation.Update(name, description);
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new ServiceCategoryTranslationUpdatedEvent(Id, normalized, name));
        }

        public void RemoveTranslation(string languageCode)
        {
            var normalized = languageCode.Trim().ToLowerInvariant();
            var translation = _translations.FirstOrDefault(t =>
                t.LanguageCode.Equals(normalized, StringComparison.OrdinalIgnoreCase));

            if (translation is null)
                throw new DomainException($"'{languageCode}' dili icin ceviri bulunamadi.");

            if (_translations.Count == 1)
                throw new DomainException("En az bir ceviri bulunmalidir.");

            _translations.Remove(translation);
            UpdatedAt = DateTime.UtcNow;
        }

        public ServiceCategoryTranslation? GetTranslation(string languageCode)
        {
            return _translations.FirstOrDefault(t =>
                t.LanguageCode.Equals(languageCode.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
        }

        public string GetName(string languageCode, string fallbackLanguage = "tr")
        {
            var translation = GetTranslation(languageCode) ?? GetTranslation(fallbackLanguage);
            return translation?.Name ?? "Unknown";
        }

        public string? GetDescription(string languageCode, string fallbackLanguage = "tr")
        {
            var translation = GetTranslation(languageCode) ?? GetTranslation(fallbackLanguage);
            return translation?.Description;
        }

        public bool IsLeaf() => !_subCategories.Any();
        public bool IsRoot() => ParentCategoryId == null;

        public ServiceCategory? GetRootCategory()
        {
            if (IsRoot()) return this;
            return ParentCategory?.GetRootCategory();
        }

        public List<ServiceCategory> GetAllDescendants()
        {
            var result = new List<ServiceCategory>();
            foreach (var sub in _subCategories)
            {
                result.Add(sub);
                result.AddRange(sub.GetAllDescendants());
            }
            return result;
        }

        public List<ServiceCategory> GetAllAncestors()
        {
            var result = new List<ServiceCategory>();
            var current = ParentCategory;
            while (current != null)
            {
                result.Add(current);
                current = current.ParentCategory;
            }
            return result;
        }

        public int GetDepth()
        {
            if (IsRoot()) return 0;
            return (ParentCategory?.GetDepth() ?? -1) + 1;
        }

        public int GetSubCategoryCount() => _subCategories.Count;
        public int GetServiceCount() => _services.Count;

        public void AddService(Service service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (service.ServiceCategoryId != Id)
                throw new DomainException("Hizmet bu kategoriye ait degil.");

            if (!IsLeaf())
                throw new DomainException("Sadece leaf kategorilere hizmet eklenebilir.");

            _services.Add(service);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveService(Service service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (!_services.Contains(service))
                throw new DomainException("Bu hizmet kategoride bulunamadi.");

            _services.Remove(service);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}