using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Fiziksel kasa (POS cekmecesi). Her kasanin ayni anda yalnizca tek
    /// acik oturumu olabilir; bu kisit CashSession uzerindeki unique
    /// filtreli index ile veritabani seviyesinde korunur.
    /// </summary>
    public class CashRegister : BaseEntity
    {
        public string Name { get; private set; }
        public bool IsActive { get; private set; }

        public Guid BusinessId { get; private set; }

        public Business Business { get; private set; }

        private CashRegister() { }

        public CashRegister(string name, Guid businessId)
            : this(Guid.NewGuid(), name, businessId)
        {
        }

        public CashRegister(Guid id, string name, Guid businessId)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            SetName(name);
            BusinessId = businessId;
            IsActive = true;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Kasa adi bos olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Kasa adi 2-100 karakter arasinda olmalidir.");

            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Deaktif kasayi tekrar aktif eder.</summary>
        public void Reactivate()
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Kasayi devre disi birakir; acik oturumu kapatmaz.</summary>
        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
