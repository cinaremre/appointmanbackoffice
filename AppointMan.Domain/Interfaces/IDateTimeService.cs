namespace AppointMan.Domain.Interfaces
{
    public interface IDateTimeService
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
        DateTime Today { get; }
        DateTimeOffset OffsetNow { get; }
        DateTimeOffset OffsetUtcNow { get; }

        bool IsWeekend(DateTime date);
        bool IsBusinessHour(DateTime date, TimeSpan startTime, TimeSpan endTime);
        int GetBusinessDaysBetween(DateTime startDate, DateTime endDate);
    }
}