using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Isletmenin geri cagirma (reengagement) kurali (dokuman Modul 13).
    /// Kural bir hizmete baglanabilir; ServiceId null ise isletmedeki
    /// tum hizmetler icin degerlendirilir.
    /// TemplateMessage degiskenleri: {{CustomerName}} {{BusinessName}}
    /// {{ServiceName}} {{LastVisitDate}} {{BookingUrl}} - gercek doldurma
    /// gonderim aninda yapilir.
    /// </summary>
    public class BusinessReengagementRule : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Null = tum hizmetler icin gecerli.</summary>
        public Guid? ServiceId { get; private set; }

        public string TemplateMessage { get; private set; }

        /// <summary>Kac gun randevusuz gecen musteri aday olur (> 0).</summary>
        public int IntervalDays { get; private set; }

        /// <summary>IntervalDays'e kac gun onceden aday listesine girer (>= 0).</summary>
        public int LeadDays { get; private set; }

        /// <summary>Bir dongrede gonderilebilecek azami mesaj (>= 1).</summary>
        public int MaxMessagesPerCycle { get; private set; }

        /// <summary>Ayni musteriye tekrar mesaj icin beklenen gun (>= 0).</summary>
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
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID bos olamaz.", nameof(serviceId));

            SetTemplateMessage(templateMessage);

            if (intervalDays <= 0)
                throw new DomainException("Geri cagirma araligi 1 gunden buyuk olmali.");

            if (leadDays < 0)
                throw new DomainException("On baslangic gunu negatif olamaz.");

            if (leadDays > intervalDays)
                throw new DomainException("On baslangic gunu araligi asamaz.");

            if (maxMessagesPerCycle < 1)
                throw new DomainException("Bir dongrede en az 1 mesaja izin verilmeli.");

            if (cooldownDays < 0)
                throw new DomainException("Bekleme suresi negatif olamaz.");

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
                throw new DomainException("Sablon mesaji bos olamaz.");

            templateMessage = templateMessage.Trim();

            if (templateMessage.Length > 1000)
                throw new DomainException("Sablon mesaji 1000 karakterden uzun olamaz.");

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
