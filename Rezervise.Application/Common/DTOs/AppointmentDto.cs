namespace Rezervise.Application.Common.DTOs
{
    public class AppointmentDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsReminderSent { get; set; }
        public DateTime? ReminderSentAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation property bilgileri
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        public string? BusinessName { get; set; }
        public string? ServiceName { get; set; }
        public string? EmployeeFullName { get; set; }
        public string? EmployeeTitle { get; set; }
    }
}