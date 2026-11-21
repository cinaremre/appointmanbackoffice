using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Paketin icindeki tek hizmet kalemi: hangi hizmetten kac seans
    /// dahil oldugunu belirtir. <see cref="ServicePackage"/> aggregate'inin
    /// cocuk entity'sidir.
    /// </summary>
    public class ServicePackageItem : BaseEntity
    {
        public Guid ServicePackageId { get; private set; }
        public Guid ServiceId { get; private set; }
        public int IncludedSessions { get; private set; }

        // Navigation
        public ServicePackage ServicePackage { get; private set; }

        private ServicePackageItem() { } // EF Core

        public ServicePackageItem(Guid servicePackageId, Guid serviceId, int includedSessions)
            : base(Guid.NewGuid())
        {
            if (servicePackageId == Guid.Empty)
                throw new ArgumentException("Paket ID bos olamaz.", nameof(servicePackageId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID bos olamaz.", nameof(serviceId));

            if (includedSessions <= 0)
                throw new DomainException("ServicePackageItem.SessionsPositive");

            ServicePackageId = servicePackageId;
            ServiceId = serviceId;
            IncludedSessions = includedSessions;
        }

        /// <summary>Dahil seans sayisini gunceller. Test/seed ve yonetim islemleri icin.</summary>
        public void SetIncludedSessions(int includedSessions)
        {
            if (includedSessions <= 0)
                throw new DomainException("ServicePackageItem.SessionsPositive");

            IncludedSessions = includedSessions;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
