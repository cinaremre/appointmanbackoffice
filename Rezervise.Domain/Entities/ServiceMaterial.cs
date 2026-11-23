using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Hizmet reçetesi: bir hizmetin tek seferlik gerçekleştirilmesinde
    /// tüketilen malzeme tanımı. Aynı (ServiceId, ProductId) çifti
    /// işletme bazında bir kez tanımlanabilir (handler tarafından kontrol edilir).
    /// </summary>
    public class ServiceMaterial : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        public Guid ServiceId { get; private set; }

        public Guid ProductId { get; private set; }

        /// <summary>Bir hizmet gerçekleştirildiğinde tüketilen miktar.</summary>
        public decimal QuantityPerService { get; private set; }

        public string Unit { get; private set; }

        private ServiceMaterial() { } // EF Core için

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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID boş olamaz.", nameof(serviceId));

            if (productId == Guid.Empty)
                throw new ArgumentException("Ürün ID boş olamaz.", nameof(productId));

            if (quantityPerService <= 0)
                throw new DomainException("Hizmet basına tüketim miktarı sıfırdan büyük olmalıdır.");

            if (quantityPerService > 99999999.99m)
                throw new DomainException("Hizmet basına tüketim miktarı çok yüksek.");

            if (string.IsNullOrWhiteSpace(unit))
                throw new DomainException("Ölçü birimi boş olamaz.");

            if (unit.Length > 20)
                throw new DomainException("Ölçü birimi 20 karakterden uzun olamaz.");

            BusinessId = businessId;
            ServiceId = serviceId;
            ProductId = productId;
            QuantityPerService = quantityPerService;
            Unit = unit.Trim();
        }

        public void SetQuantityPerService(decimal quantityPerService)
        {
            if (quantityPerService <= 0)
                throw new DomainException("Hizmet basına tüketim miktarı sıfırdan büyük olmalıdır.");

            if (quantityPerService > 99999999.99m)
                throw new DomainException("Hizmet basına tüketim miktarı çok yüksek.");

            QuantityPerService = quantityPerService;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
