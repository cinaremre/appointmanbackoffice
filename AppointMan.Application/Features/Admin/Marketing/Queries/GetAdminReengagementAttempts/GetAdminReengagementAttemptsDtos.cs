using AppointMan.Domain.Enums;

namespace AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminReengagementAttempts
{
    /// <summary>Tek bir geri cagirma denemesinin yonetici gorunumu.</summary>
    public class AdminReengagementAttemptDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid RuleId { get; set; }

        /// <summary>Gonderim varsa bildirim kaydinin kimligi.</summary>
        public Guid? NotificationId { get; set; }

        public DateTime TriggeredAt { get; set; }
        public EnumReengagementStatus Status { get; set; }

        /// <summary>Donusum gerceklesirse olusan randevunun kimligi.</summary>
        public Guid? ConvertedAppointmentId { get; set; }
    }

    /// <summary>Filtreye uyan geri cagirma denemeleri (tarih tersten sirali).</summary>
    public class AdminReengagementAttemptListDto
    {
        public IReadOnlyList<AdminReengagementAttemptDto> Items { get; set; }
            = Array.Empty<AdminReengagementAttemptDto>();
    }
}
