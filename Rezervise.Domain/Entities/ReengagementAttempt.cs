using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Tek bir geri çağırma denemesi (doküman Modül 13).
    /// Aday bulunur (Eligible), bildirim gönderilir (Notified) ve dönüşüm
    /// randevuya bağlanır (Converted).
    /// </summary>
    public class ReengagementAttempt : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Müşterinin User ID'si.</summary>
        public Guid CustomerId { get; private set; }

        public Guid ServiceId { get; private set; }
        public Guid RuleId { get; private set; }

        /// <summary>Gönderim varsa bildirim kaydının ID'si.</summary>
        public Guid? NotificationId { get; private set; }

        public DateTime TriggeredAt { get; private set; }
        public EnumReengagementStatus Status { get; private set; }

        /// <summary>Dönüşüm gerçekleşirse oluşan randevunun ID'si.</summary>
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
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Müşteri ID boş olamaz.", nameof(customerId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID boş olamaz.", nameof(serviceId));

            if (ruleId == Guid.Empty)
                throw new ArgumentException("Kural ID boş olamaz.", nameof(ruleId));

            BusinessId = businessId;
            CustomerId = customerId;
            ServiceId = serviceId;
            RuleId = ruleId;
            TriggeredAt = DateTime.SpecifyKind(triggeredAt, DateTimeKind.Utc);
            Status = EnumReengagementStatus.Eligible;
        }

        /// <summary>Aday için bildirim gönderildi olarak işaretler.</summary>
        public void MarkNotified(Guid? notificationId = null)
        {
            if (Status != EnumReengagementStatus.Eligible)
                return;

            Status = EnumReengagementStatus.Notified;
            NotificationId = notificationId ?? NotificationId;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Geri çağırma randevuya dönüştü.</summary>
        public void MarkConverted(Guid appointmentId)
        {
            if (appointmentId == Guid.Empty)
                throw new ArgumentException("Randevu ID boş olamaz.", nameof(appointmentId));

            if (Status != EnumReengagementStatus.Notified)
                return;

            Status = EnumReengagementStatus.Converted;
            ConvertedAppointmentId = appointmentId;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>İzin/molal nedeniyle gönderim engellendi.</summary>
        public void MarkSuppressed()
        {
            if (Status != EnumReengagementStatus.Eligible)
                return;

            Status = EnumReengagementStatus.Suppressed;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Cooldown süresi devrede; deneme ertelendi.</summary>
        public void MarkCooldown()
        {
            if (Status != EnumReengagementStatus.Eligible)
                return;

            Status = EnumReengagementStatus.Cooldown;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
