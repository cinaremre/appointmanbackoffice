namespace Rezervise.Application.Common.DTOs
{
    public class BlacklistEntryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; }
        public string UserEmail { get; set; }
        public string? UserPhoneNumber { get; set; }
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; }
        public string? Reason { get; set; }
        public int AppointmentCount { get; set; }
        public DateTime BlacklistedAt { get; set; }
        public DateTime? RemovedAt { get; set; }
        public string? RemovedBy { get; set; }
        public string? RemoveReason { get; set; }
        public bool IsActive { get; set; }
    }
}