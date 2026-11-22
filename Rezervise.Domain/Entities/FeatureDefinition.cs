using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Platformda tanimli bir ucretli/entitlement ozelligi (dokuman Modul 14).
    /// Kodu benzersizdir; plan ve override kayitlari bu tanima baglanir.
    /// </summary>
    public class FeatureDefinition : BaseEntity
    {
        /// <summary>
        /// Ozelligin benzersiz kodu (orn. "WHATSAPP_REMINDER").
        /// Uygulama kodu FeatureCodes sabitlerini kullanir.
        /// </summary>
        public string Code { get; private set; }

        public string Name { get; private set; }

        public string? Description { get; private set; }

        /// <summary>Devre disi ozellik hicbir kaynaktan acik donmez.</summary>
        public bool IsActive { get; private set; }

        private FeatureDefinition() { } // EF Core

        public FeatureDefinition(string code, string name, string? description = null)
            : this(Guid.NewGuid(), code, name, description)
        {
        }

        public FeatureDefinition(Guid id, string code, string name, string? description = null)
            : base(id)
        {
            SetCode(code);
            SetName(name);
            Description = description;
            IsActive = true;
        }

        public void SetCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new DomainException("Ozellik kodu bos olamaz.");

            code = code.Trim();

            if (code.Length < 2 || code.Length > 64)
                throw new DomainException("Ozellik kodu 2-64 karakter arasinda olmali.");

            Code = code;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Ozellik adi bos olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Ozellik adi 2-100 karakter arasinda olmali.");

            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetDescription(string? description)
        {
            if (description != null && description.Length > 500)
                throw new DomainException("Ozellik aciklamasi 500 karakterden uzun olamaz.");

            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
