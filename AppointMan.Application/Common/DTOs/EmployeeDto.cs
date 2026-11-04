namespace AppointMan.Application.Common.DTOs
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Title { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public Guid BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailableForBooking { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FullName { get; set; }
        public string DisplayName { get; set; }
    }
}