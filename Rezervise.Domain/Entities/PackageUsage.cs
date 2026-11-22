using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Paket hak hareketi (dokuman Modul 5). Her hareket tek bir randevuya
    /// baglanir: Reserve ile hak ayrilir, Consume ile tuketilir, Release ile
    /// iptal edilen randevunun hakki geri verilir.
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
                throw new ArgumentException("Musteri paketi ID bos olamaz.", nameof(customerPackageId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID bos olamaz.", nameof(serviceId));

            if (appointmentId == Guid.Empty)
                throw new ArgumentException("Randevu ID bos olamaz.", nameof(appointmentId));

            CustomerPackageId = customerPackageId;
            ServiceId = serviceId;
            AppointmentId = appointmentId;
            UsageType = usageType;
            Status = EnumPackageUsageStatus.Active;
            CreatedAt = now;
        }

        /// <summary>
        /// Randevu icin paketten hak ayirir (aktif rezervasyon hareketi).
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
        /// Tamamlanan randevu icin ayridigi hakki tuketim hareketine cevirir.
        /// Cagrilmadan once ayni randevunun aktif Reservation hareketi
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
        /// Aktif hareketi iptal eder; hak yeniden kullanilabilir olur.
        /// Tuketim (Consume) hareketi, Reservation hareketinin yerine gecmeden
        /// once bu metotla eski hareket iptal edilir.
        /// </summary>
        public void Cancel(DateTime now)
        {
            if (Status != EnumPackageUsageStatus.Active)
                throw new DomainException("PackageUsage.NotActive");

            Status = EnumPackageUsageStatus.Cancelled;
            UpdatedAt = now;
        }

        /// <summary>
        /// Randevu iptalinde aktif Reservation hareketini Release'e cevirir:
        /// hareket turu Release, durumu Cancelled olur ve kalan hak hesabina
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
