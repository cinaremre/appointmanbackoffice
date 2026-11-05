using AppointMan.Domain.Enums;

namespace AppointMan.Application.Common.DTOs
{
    public class ServiceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public int DurationInMinutes { get; set; }
        public Guid ServiceCategoryId { get; set; }
        public string? ServiceCategoryName { get; set; }
        public bool IsActive { get; set; }
        public Guid BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}