using AppointMan.Domain.Enums;

namespace AppointMan.Application.Common.DTOs
{
    public class AppointmentHistoryDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public EnumAppointmentStatus OldStatus { get; set; }
        public EnumAppointmentStatus NewStatus { get; set; }
        public string? ChangeReason { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? ChangedBy { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
        public string OldStatusName { get; set; }
        public string NewStatusName { get; set; }
    }
}