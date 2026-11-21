using AppointMan.Domain.Enums;

namespace AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentDetail
{
    /// <summary>Randevu detayi ve kisa gecmis (dokuman Modul 12).</summary>
    public class AdminAppointmentDetailDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public EnumAppointmentStatus Status { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Durum gecmisi; en yeni kayit basta, en fazla 20 og.</summary>
        public IReadOnlyList<AdminAppointmentHistoryDto> History { get; set; } = Array.Empty<AdminAppointmentHistoryDto>();
    }

    /// <summary>Gecmis ogesinin ozeti.</summary>
    public class AdminAppointmentHistoryDto
    {
        public Guid Id { get; set; }
        public EnumAppointmentStatus OldStatus { get; set; }
        public EnumAppointmentStatus NewStatus { get; set; }
        public string? ChangeReason { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
