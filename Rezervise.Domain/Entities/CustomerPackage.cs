using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Musterinin satin aldigi paket ornegi (dokuman Modul 5).
    /// Kalan hak entity'de TUTULMAZ; Reserve/Consume/Release hareketleri
    /// (<see cref="PackageUsage"/>) uzerinden handler'da toplanarak hesaplanir.
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
        /// Yeni paket satisi. Gecerlilik bitisi satin alma anindan
        /// <paramref name="validityDays"/> gun sonra hesaplanir.
        /// </summary>
        /// <param name="businessId">Isletme ID.</param>
        /// <param name="customerId">Musteri (User) ID.</param>
        /// <param name="servicePackageId">Satin alinan paket tanimi ID.</param>
        /// <param name="validityDays">Paketin gecerlilik suresi (gun).</param>
        /// <param name="now">Islem zamani (UTC).</param>
        public CustomerPackage(
            Guid businessId,
            Guid customerId,
            Guid servicePackageId,
            int validityDays,
            DateTime now)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Musteri ID bos olamaz.", nameof(customerId));

            if (servicePackageId == Guid.Empty)
                throw new ArgumentException("Paket tanimi ID bos olamaz.", nameof(servicePackageId));

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
        /// Paket uzerinde hak kullanilabilir mi? Durum Active olmali ve
        /// gecerlilik suresi henuz dolmamis olmali.
        /// </summary>
        /// <param name="now">Referans UTC zaman.</param>
        public bool IsUsable(DateTime now)
        {
            return Status == EnumCustomerPackageStatus.Active && ExpiresAt > now;
        }

        /// <summary>
        /// Gecerlilik suresi dolmussa paketi Expired isaretler.
        /// Henuz dolmamsa veya durum zaten Active degilse hicbir sey yapmaz.
        /// </summary>
        /// <param name="now">Referans UTC zaman.</param>
        public void ExpireIfDue(DateTime now)
        {
            if (Status != EnumCustomerPackageStatus.Active || ExpiresAt > now)
                return;

            Status = EnumCustomerPackageStatus.Expired;
            UpdatedAt = now;
        }

        /// <summary>Paketi iptal eder. Yalnizca Active paketler iptal edilebilir.</summary>
        /// <param name="now">Islem zamani (UTC).</param>
        public void Cancel(DateTime now)
        {
            if (Status != EnumCustomerPackageStatus.Active)
                throw new DomainException("CustomerPackage.NotActive");

            Status = EnumCustomerPackageStatus.Cancelled;
            UpdatedAt = now;
        }
    }
}
