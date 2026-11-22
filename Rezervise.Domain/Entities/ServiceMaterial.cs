using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Hizmet recetesi: bir hizmetin tek seferlik gerceklestirilmesinde
    /// tuketilen malzeme tanimi. Ayni (ServiceId, ProductId) cifti
    /// isletme bazinda bir kez tanimlanabilir (handler tarafindan kontrol edilir).
    /// </summary>
    public class ServiceMaterial : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        public Guid ServiceId { get; private set; }

        public Guid ProductId { get; private set; }

        /// <summary>Bir hizmet gerceklestirildiginde tuketilen miktar.</summary>
        public decimal QuantityPerService { get; private set; }

        public string Unit { get; private set; }

        private ServiceMaterial() { } // EF Core icin

        public ServiceMaterial(
            Guid businessId,
            Guid serviceId,
            Guid productId,
            decimal quantityPerService,
            string unit = "adet")
            : this(Guid.NewGuid(), businessId, serviceId, productId, quantityPerService, unit)
        {
        }

        public ServiceMaterial(
            Guid id,
            Guid businessId,
            Guid serviceId,
            Guid productId,
            decimal quantityPerService,
            string unit = "adet")
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID bos olamaz.", nameof(serviceId));

            if (productId == Guid.Empty)
                throw new ArgumentException("Urun ID bos olamaz.", nameof(productId));

            if (quantityPerService <= 0)
                throw new DomainException("Hizmet basina tuketim miktari sifirdan buyuk olmalidir.");

            if (quantityPerService > 99999999.99m)
                throw new DomainException("Hizmet basina tuketim miktari cok yuksek.");

            if (string.IsNullOrWhiteSpace(unit))
                throw new DomainException("Olcu birimi bos olamaz.");

            if (unit.Length > 20)
                throw new DomainException("Olcu birimi 20 karakterden uzun olamaz.");

            BusinessId = businessId;
            ServiceId = serviceId;
            ProductId = productId;
            QuantityPerService = quantityPerService;
            Unit = unit.Trim();
        }

        public void SetQuantityPerService(decimal quantityPerService)
        {
            if (quantityPerService <= 0)
                throw new DomainException("Hizmet basina tuketim miktari sifirdan buyuk olmalidir.");

            if (quantityPerService > 99999999.99m)
                throw new DomainException("Hizmet basina tuketim miktari cok yuksek.");

            QuantityPerService = quantityPerService;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
