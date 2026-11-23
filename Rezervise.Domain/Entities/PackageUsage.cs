using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Paket hak hareketi (doküman Modül 5). Her hareket tek bir randevuya
    /// bağlanır: Reserve ile hak ayrılır, Consume ile tüketilir, Release ile
    /// iptal edilen randevunun hakkı geri verilir.
    /// </summary>
    public class PackageUsage : BaseEntity
    {
        public Guid CustomerPackageId { get; private set; }
        public Guid ServiceId { get; private set; }
        public Guid AppointmentId { get; private set; }

        public EnumPackageUsageType UsageType { get; private set; }
        public EnumPackageUsageStatus Status { get; private set; }

        private PackageUsage() { } // EF Core

        private PackageUsage(
            Guid id,
            Guid customerPackageId,
            Guid serviceId,
            Guid appointmentId,
            EnumPackageUsageType usageType,
            DateTime now)
            : base(id)
        {
            if (customerPackageId == Guid.Empty)
                throw new ArgumentException("Müşteri paketi ID boş olamaz.", nameof(customerPackageId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID boş olamaz.", nameof(serviceId));

            if (appointmentId == Guid.Empty)
                throw new ArgumentException("Randevu ID boş olamaz.", nameof(appointmentId));

            CustomerPackageId = customerPackageId;
            ServiceId = serviceId;
            AppointmentId = appointmentId;
            UsageType = usageType;
            Status = EnumPackageUsageStatus.Active;
            CreatedAt = now;
        }

        /// <summary>
        /// Randevu için paketten hak ayırır (aktif rezervasyon hareketi).
        /// </summary>
        public static PackageUsage Reserve(
            Guid customerPackageId,
            Guid serviceId,
            Guid appointmentId,
            DateTime now)
        {
            return new PackageUsage(
                Guid.NewGuid(),
                customerPackageId,
                serviceId,
                appointmentId,
                EnumPackageUsageType.Reservation,
                now);
        }

        /// <summary>
        /// Tamamlanan randevu için ayrıdığı hakkı tüketim hareketine çevirir.
        /// Çağrılmadan önce aynı randevunun aktif Reservation hareketi
        /// <see cref="Cancel"/> ile iptal edilmelidir.
        /// </summary>
        public static PackageUsage Consume(
            Guid customerPackageId,
            Guid serviceId,
            Guid appointmentId,
            DateTime now)
        {
            return new PackageUsage(
                Guid.NewGuid(),
                customerPackageId,
                serviceId,
                appointmentId,
                EnumPackageUsageType.Consumption,
                now);
        }

        /// <summary>
        /// Aktif hareketi iptal eder; hak yeniden kullanılabilir olur.
        /// Tüketim (Consume) hareketi, Reservation hareketinin yerine geçmeden
        /// önce bu metotla eski hareket iptal edilir.
        /// </summary>
        public void Cancel(DateTime now)
        {
            if (Status != EnumPackageUsageStatus.Active)
                throw new DomainException("PackageUsage.NotActive");

            Status = EnumPackageUsageStatus.Cancelled;
            UpdatedAt = now;
        }

        /// <summary>
        /// Randevu iptalinde aktif Reservation hareketini Release'e çevirir:
        /// hareket turu Release, durumu Cancelled olur ve kalan hak hesabına
        /// bir daha girmez.
        /// </summary>
        public void Release(DateTime now)
        {
            if (UsageType != EnumPackageUsageType.Reservation)
                throw new DomainException("PackageUsage.ReleaseRequiresReservation");

            if (Status != EnumPackageUsageStatus.Active)
                throw new DomainException("PackageUsage.NotActive");

            UsageType = EnumPackageUsageType.Release;
            Status = EnumPackageUsageStatus.Cancelled;
            UpdatedAt = now;
        }
    }
}
