namespace Rezervise.Application.Common.DTOs
{
    public class WorkingHourDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsClosed { get; set; }
        public string DayName { get; set; }
    }
}