using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    public class ServiceCategoryTranslation : BaseEntity
    {
        public Guid ServiceCategoryId { get; private set; }
        public ServiceCategory ServiceCategory { get; private set; }

        public string LanguageCode { get; private set; }
        public string Name { get; private set; }
        public string? Description { get; private set; }

        private ServiceCategoryTranslation() { }

        public ServiceCategoryTranslation(
    Guid serviceCategoryId,
    string languageCode,
    string name,
    string? description = null)
    : this(Guid.NewGuid(), serviceCategoryId, languageCode, name, description)
        {
        }
        public ServiceCategoryTranslation(
    Guid id,
    Guid serviceCategoryId,
    string languageCode,
    string name,
    string? description = null)
    : base(id)
        {
            if (serviceCategoryId == Guid.Empty)
                throw new ArgumentException("Kategori ID bos olamaz.", nameof(serviceCategoryId));

            ServiceCategoryId = serviceCategoryId;
            LanguageCode = NormalizeLanguageCode(languageCode);
            SetName(name);
            SetDescription(description);
        }

        public void Update(string name, string? description = null)
        {
            SetName(name);
            SetDescription(description);
            UpdatedAt = DateTime.UtcNow;
        }

        private void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Kategori adi bos olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Kategori adi 2-100 karakter arasinda olmalidir.");

            Name = name.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        private void SetDescription(string? description)
        {
            if (description != null && description.Length > 500)
                throw new DomainException("Aciklama 500 karakterden uzun olamaz.");

            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                throw new DomainException("Dil kodu bos olamaz.");

            languageCode = languageCode.Trim().ToLowerInvariant();

            if (languageCode.Length != 2 && languageCode.Length != 5)
                throw new DomainException("Dil kodu 2 karakter (tr) veya 5 karakter (tr-TR) olmalidir.");

            if (languageCode.Length == 5 && languageCode[2] != '-')
                throw new DomainException("5 karakterli dil kodu 'xx-XX' formatinda olmalidir.");

            return languageCode;
        }
    }
}