namespace AppointMan.Application.Common.DTOs
{
    public class FavoriteDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessAddress { get; set; }
        public string? Note { get; set; }
        public DateTime AddedAt { get; set; }
    }
}