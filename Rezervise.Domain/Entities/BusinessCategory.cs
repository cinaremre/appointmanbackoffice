using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    public class BusinessCategory : BaseEntity
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public string? IconUrl { get; private set; }
        public string? ImageUrl { get; private set; }
        public int DisplayOrder { get; private set; }
        public bool IsActive { get; private set; }
        public EnumBusinessType? CorrespondingBusinessType { get; private set; }
        public Guid? ParentCategoryId { get; private set; }

        // Navigation Properties
        public BusinessCategory? ParentCategory { get; private set; }

        private readonly List<BusinessCategory> _subCategories = new();
        public IReadOnlyCollection<BusinessCategory> SubCategories => _subCategories.AsReadOnly();

        private readonly List<Business> _businesses = new();
        public IReadOnlyCollection<Business> Businesses => _businesses.AsReadOnly();

        private BusinessCategory() { } // EF Core

        public BusinessCategory(string name, int displayOrder = 0, EnumBusinessType? businessType = null)
     : this(Guid.NewGuid(), name, displayOrder, businessType)
        {
        }
        public BusinessCategory(Guid id, string name, int displayOrder = 0, EnumBusinessType? businessType = null)
    : base(id)
        {
            SetName(name);
            DisplayOrder = displayOrder;
            IsActive = true;
            CorrespondingBusinessType = businessType;
        }
        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Kategori adi bos olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Kategori adi 2-100 karakter arasinda olmalidir.");

            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetDescription(string? description)
        {
            if (description != null && description.Length > 500)
                throw new DomainException("Açiklama 500 karakterden uzun olamaz.");

            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetIconUrl(string? iconUrl)
        {
            if (iconUrl != null && !Uri.IsWellFormedUriString(iconUrl, UriKind.Absolute))
                throw new DomainException("Geçersiz URL formati.");

            IconUrl = iconUrl;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetImageUrl(string? imageUrl)
        {
            if (imageUrl != null && !Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                throw new DomainException("Geçersiz URL formati.");

            ImageUrl = imageUrl;
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
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetParentCategory(BusinessCategory? parentCategory)
        {
            if (parentCategory != null && parentCategory.Id == Id)
                throw new DomainException("Kategori kendisinin alt kategorisi olamaz.");

            if (parentCategory != null && parentCategory.ParentCategoryId == Id)
                throw new DomainException("Döngüsel kategori iliskisi olusturulamaz.");

            // Eski parent'tan çikar
            if (ParentCategory != null)
            {
                ParentCategory._subCategories.Remove(this);
            }

            ParentCategory = parentCategory;
            ParentCategoryId = parentCategory?.Id;

            // Yeni parent'a ekle
            if (parentCategory != null && !parentCategory._subCategories.Contains(this))
            {
                parentCategory._subCategories.Add(this);
            }

            UpdatedAt = DateTime.UtcNow;
        }

        public void AddSubCategory(BusinessCategory subCategory)
        {
            if (subCategory == null)
                throw new ArgumentNullException(nameof(subCategory));

            if (subCategory.Id == Id)
                throw new DomainException("Kategori kendisini alt kategori olarak ekleyemez.");

            if (_subCategories.Contains(subCategory))
                throw new DomainException("Bu alt kategori zaten eklenmis.");

            subCategory.SetParentCategory(this);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveSubCategory(BusinessCategory subCategory)
        {
            if (subCategory == null)
                throw new ArgumentNullException(nameof(subCategory));

            if (!_subCategories.Contains(subCategory))
                throw new DomainException("Bu alt kategori bulunamadi.");

            subCategory.SetParentCategory(null);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddBusiness(Business business)
        {
            if (business == null)
                throw new ArgumentNullException(nameof(business));

            if (!_businesses.Contains(business))
            {
                _businesses.Add(business);
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void RemoveBusiness(Business business)
        {
            if (business == null)
                throw new ArgumentNullException(nameof(business));

            if (_businesses.Contains(business))
            {
                _businesses.Remove(business);
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public bool IsParentOf(BusinessCategory category)
        {
            if (category == null)
                return false;

            var current = category.ParentCategory;
            while (current != null)
            {
                if (current.Id == Id)
                    return true;
                current = current.ParentCategory;
            }
            return false;
        }

        public List<BusinessCategory> GetAllSubCategories()
        {
            var result = new List<BusinessCategory>();
            foreach (var sub in _subCategories)
            {
                result.Add(sub);
                result.AddRange(sub.GetAllSubCategories());
            }
            return result;
        }

        public int GetSubCategoryCount()
        {
            return _subCategories.Count;
        }

        public int GetBusinessCount()
        {
            return _businesses.Count;
        }
    }
}