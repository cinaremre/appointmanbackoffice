using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Paketin içindeki tek hizmet kalemi: hangi hizmetten kaç seans
    /// dahil öldüğünü belirtir. <see cref="ServicePackage"/> aggregate'inin
    /// çocuk entity'sidir.
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
                throw new ArgumentException("Paket ID boş olamaz.", nameof(servicePackageId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID boş olamaz.", nameof(serviceId));

            if (includedSessions <= 0)
                throw new DomainException("ServicePackageItem.SessionsPositive");

            ServicePackageId = servicePackageId;
            ServiceId = serviceId;
            IncludedSessions = includedSessions;
        }

        /// <summary>Dahil seans sayısını günceller. Test/seed ve yönetim işlemleri için.</summary>
        public void SetIncludedSessions(int includedSessions)
        {
            if (includedSessions <= 0)
                throw new DomainException("ServicePackageItem.SessionsPositive");

            IncludedSessions = includedSessions;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
