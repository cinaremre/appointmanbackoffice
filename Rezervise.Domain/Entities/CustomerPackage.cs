using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Müşterinin satın aldığı paket örneği (doküman Modül 5).
    /// Kalan hak entity'de TUTULMAZ; Reserve/Consume/Release hareketleri
    /// (<see cref="PackageUsage"/>) üzerinden handler'da toplanarak hesaplanır.
    /// </summary>
    public class CustomerPackage : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CustomerId { get; private set; }
        public Guid ServicePackageId { get; private set; }

        public DateTime PurchasedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public EnumCustomerPackageStatus Status { get; private set; }

        // Navigation
        public ServicePackage ServicePackage { get; private set; }

        private CustomerPackage() { } // EF Core

        /// <summary>
        /// Yeni paket satışı. Geçerlilik bitişi satın alma anından
        /// <paramref name="validityDays"/> gün sonra hesaplanır.
        /// </summary>
        /// <param name="businessId">İşletme ID.</param>
        /// <param name="customerId">Müşteri (User) ID.</param>
        /// <param name="servicePackageId">Satın alınan paket tanımı ID.</param>
        /// <param name="validityDays">Paketin geçerlilik süresi (gün).</param>
        /// <param name="now">İşlem zamanı (UTC).</param>
        public CustomerPackage(
            Guid businessId,
            Guid customerId,
            Guid servicePackageId,
            int validityDays,
            DateTime now)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Müşteri ID boş olamaz.", nameof(customerId));

            if (servicePackageId == Guid.Empty)
                throw new ArgumentException("Paket tanımı ID boş olamaz.", nameof(servicePackageId));

            if (validityDays < 1)
                throw new DomainException("CustomerPackage.ValidityDaysPositive");

            if (now == default)
                throw new DomainException("CustomerPackage.PurchaseTimeRequired");

            BusinessId = businessId;
            CustomerId = customerId;
            ServicePackageId = servicePackageId;
            PurchasedAt = now;
            ExpiresAt = now.AddDays(validityDays);
            Status = EnumCustomerPackageStatus.Active;
        }

        /// <summary>
        /// Paket üzerinde hak kullanılabilir mi? Durum Active olmalı ve
        /// geçerlilik süresi henüz dolmamış olmalı.
        /// </summary>
        /// <param name="now">Referans UTC zaman.</param>
        public bool IsUsable(DateTime now)
        {
            return Status == EnumCustomerPackageStatus.Active && ExpiresAt > now;
        }

        /// <summary>
        /// Geçerlilik süresi dolmuşsa paketi Expired işaretler.
        /// Henüz dolmamsa veya durum zaten Active değilse hiçbir şey yapmaz.
        /// </summary>
        /// <param name="now">Referans UTC zaman.</param>
        public void ExpireIfDue(DateTime now)
        {
            if (Status != EnumCustomerPackageStatus.Active || ExpiresAt > now)
                return;

            Status = EnumCustomerPackageStatus.Expired;
            UpdatedAt = now;
        }

        /// <summary>Paketi iptal eder. Yalnızca Active paketler iptal edilebilir.</summary>
        /// <param name="now">İşlem zamanı (UTC).</param>
        public void Cancel(DateTime now)
        {
            if (Status != EnumCustomerPackageStatus.Active)
                throw new DomainException("CustomerPackage.NotActive");

            Status = EnumCustomerPackageStatus.Cancelled;
            UpdatedAt = now;
        }
    }
}
