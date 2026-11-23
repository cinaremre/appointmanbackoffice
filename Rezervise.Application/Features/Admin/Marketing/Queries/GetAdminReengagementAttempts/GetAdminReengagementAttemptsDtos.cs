using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminReengagementAttempts
{
    /// <summary>Tek bir geri çağırma denemesinin yönetici görünümü.</summary>
    public class AdminReengagementAttemptDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid RuleId { get; set; }

        /// <summary>Gönderim varsa bildirim kaydının kimliği.</summary>
        public Guid? NotificationId { get; set; }

        public DateTime TriggeredAt { get; set; }
        public EnumReengagementStatus Status { get; set; }

        /// <summary>Dönüşüm gerçekleşirse oluşan randevunun kimliği.</summary>
        public Guid? ConvertedAppointmentId { get; set; }
    }

    /// <summary>Filtreye uyan geri çağırma denemeleri (tarih tersten sıralı).</summary>
    public class AdminReengagementAttemptListDto
    {
        public IReadOnlyList<AdminReengagementAttemptDto> Items { get; set; }
            = Array.Empty<AdminReengagementAttemptDto>();
    }
}
