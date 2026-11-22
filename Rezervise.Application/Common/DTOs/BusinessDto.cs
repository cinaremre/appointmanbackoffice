using Rezervise.Domain.Enums;

namespace Rezervise.Application.Common.DTOs
{
    public class BusinessDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public EnumBusinessType BusinessType { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public decimal? Rating { get; set; }
        public int TotalReviews { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}