namespace AppointMan.Application.Common.DTOs
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? AppointmentId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; }
        public bool IsVisible { get; set; }
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        public string? BusinessName { get; set; }
    }
}