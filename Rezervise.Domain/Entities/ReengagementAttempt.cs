using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Tek bir geri cagirma denemesi (dokuman Modul 13).
    /// Aday bulunur (Eligible), bildirim gonderilir (Notified) ve donusum
    /// randevuya baglanir (Converted).
    /// </summary>
    public class ReengagementAttempt : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Musterinin User ID'si.</summary>
        public Guid CustomerId { get; private set; }

        public Guid ServiceId { get; private set; }
        public Guid RuleId { get; private set; }

        /// <summary>Gonderim varsa bildirim kaydinin ID'si.</summary>
        public Guid? NotificationId { get; private set; }

        public DateTime TriggeredAt { get; private set; }
        public EnumReengagementStatus Status { get; private set; }

        /// <summary>Donusum gerceklesirse olusan randevunun ID'si.</summary>
        public Guid? ConvertedAppointmentId { get; private set; }

        // Navigation Properties
        public BusinessReengagementRule Rule { get; private set; }

        private ReengagementAttempt() { } // EF Core

        public ReengagementAttempt(
            Guid businessId,
            Guid customerId,
            Guid serviceId,
            Guid ruleId,
            DateTime triggeredAt)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Musteri ID bos olamaz.", nameof(customerId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID bos olamaz.", nameof(serviceId));

            if (ruleId == Guid.Empty)
                throw new ArgumentException("Kural ID bos olamaz.", nameof(ruleId));

            BusinessId = businessId;
            CustomerId = customerId;
            ServiceId = serviceId;
            RuleId = ruleId;
            TriggeredAt = DateTime.SpecifyKind(triggeredAt, DateTimeKind.Utc);
            Status = EnumReengagementStatus.Eligible;
        }

        /// <summary>Aday icin bildirim gonderildi olarak isaretler.</summary>
        public void MarkNotified(Guid? notificationId = null)
        {
            if (Status != EnumReengagementStatus.Eligible)
                return;

            Status = EnumReengagementStatus.Notified;
            NotificationId = notificationId ?? NotificationId;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Geri cagirma randevuya donustu.</summary>
        public void MarkConverted(Guid appointmentId)
        {
            if (appointmentId == Guid.Empty)
                throw new ArgumentException("Randevu ID bos olamaz.", nameof(appointmentId));

            if (Status != EnumReengagementStatus.Notified)
                return;

            Status = EnumReengagementStatus.Converted;
            ConvertedAppointmentId = appointmentId;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Izin/molal nedeniyle gonderim engellendi.</summary>
        public void MarkSuppressed()
        {
            if (Status != EnumReengagementStatus.Eligible)
                return;

            Status = EnumReengagementStatus.Suppressed;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Cooldown suresi devrede; deneme ertelendi.</summary>
        public void MarkCooldown()
        {
            if (Status != EnumReengagementStatus.Eligible)
                return;

            Status = EnumReengagementStatus.Cooldown;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
