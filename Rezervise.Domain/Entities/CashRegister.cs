using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Fiziksel kasa (POS çekmecesi). Her kasanın aynı anda yalnızca tek
    /// açık oturumu olabilir; bu kısıt CashSession üzerindeki unique
    /// filtreli index ile veritabanı seviyesinde korunur.
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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            SetName(name);
            BusinessId = businessId;
            IsActive = true;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Kasa adı boş olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Kasa adı 2-100 karakter arasında olmalıdır.");

            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Deaktif kasayı tekrar aktif eder.</summary>
        public void Reactivate()
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Kasayı devre dışı bırakır; açık oturumu kapatmaz.</summary>
        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
