using Rezervise.Domain.Enums;

namespace Rezervise.Application.Common.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public EnumNotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime SentAt { get; set; }
        public string? ActionUrl { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}