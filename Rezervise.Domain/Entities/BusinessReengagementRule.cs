using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İşletmenin geri çağırma (reengagement) kuralı (doküman Modül 13).
    /// Kural bir hizmete bağlanabilir; ServiceId null ise işletmedeki
    /// tüm hizmetler için değerlendirilir.
    /// TemplateMessage değişkenleri: {{CustomerName}} {{BusinessName}}
    /// {{ServiceName}} {{LastVisitDate}} {{BookingUrl}} - gerçek doldurma
    /// gönderim anında yapılır.
    /// </summary>
    public class BusinessReengagementRule : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Null = tüm hizmetler için geçerli.</summary>
        public Guid? ServiceId { get; private set; }

        public string TemplateMessage { get; private set; }

        /// <summary>Kaç gün randevusuz geçen müşteri aday olur (> 0).</summary>
        public int IntervalDays { get; private set; }

        /// <summary>IntervalDays'e kaç gün önceden aday listesine girer (>= 0).</summary>
        public int LeadDays { get; private set; }

        /// <summary>Bir döngrede gönderilebilecek azami mesaj (>= 1).</summary>
        public int MaxMessagesPerCycle { get; private set; }

        /// <summary>Aynı müşteriye tekrar mesaj için beklenen gün (>= 0).</summary>
        public int CooldownDays { get; private set; }

        public bool IsEnabled { get; private set; }

        // Navigation Properties
        public Business Business { get; private set; }
        public Service? Service { get; private set; }

        private BusinessReengagementRule() { } // EF Core

        public BusinessReengagementRule(
            Guid businessId,
            Guid? serviceId,
            string templateMessage,
            int intervalDays,
            int leadDays,
            int maxMessagesPerCycle,
            int cooldownDays,
            bool isEnabled = true)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID boş olamaz.", nameof(serviceId));

            SetTemplateMessage(templateMessage);

            if (intervalDays <= 0)
                throw new DomainException("Geri çağırma aralığı 1 günden büyük olmalı.");

            if (leadDays < 0)
                throw new DomainException("On başlangıç günü negatif olamaz.");

            if (leadDays > intervalDays)
                throw new DomainException("On başlangıç günü aralığı aşamaz.");

            if (maxMessagesPerCycle < 1)
                throw new DomainException("Bir döngrede en az 1 mesaja izin verilmeli.");

            if (cooldownDays < 0)
                throw new DomainException("Bekleme süresi negatif olamaz.");

            BusinessId = businessId;
            ServiceId = serviceId;
            IntervalDays = intervalDays;
            LeadDays = leadDays;
            MaxMessagesPerCycle = maxMessagesPerCycle;
            CooldownDays = cooldownDays;
            IsEnabled = isEnabled;
        }

        public void SetTemplateMessage(string templateMessage)
        {
            if (string.IsNullOrWhiteSpace(templateMessage))
                throw new DomainException("Şablon mesajı boş olamaz.");

            templateMessage = templateMessage.Trim();

            if (templateMessage.Length > 1000)
                throw new DomainException("Şablon mesajı 1000 karakterden uzun olamaz.");

            TemplateMessage = templateMessage;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Enable()
        {
            if (IsEnabled)
                return;

            IsEnabled = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Disable()
        {
            if (!IsEnabled)
                return;

            IsEnabled = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
