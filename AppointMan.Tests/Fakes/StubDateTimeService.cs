using AppointMan.Domain.Interfaces;

namespace AppointMan.Tests.Fakes;

public sealed class StubDateTimeService : IDateTimeService
{
    public StubDateTimeService(DateTime utcNow, double timeZoneOffsetHours = 3)
    {
        UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        TimeZoneOffsetHours = timeZoneOffsetHours;
    }

    public double TimeZoneOffsetHours { get; }

    public DateTime UtcNow { get; set; }
    public DateTime Now => ToBusinessLocalTime(UtcNow);
    public DateTime Today => Now.Date;
    public DateTimeOffset OffsetNow => new(Now, TimeSpan.FromHours(TimeZoneOffsetHours));
    public DateTimeOffset OffsetUtcNow => new(UtcNow);

    public bool IsWeekend(DateTime date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public bool IsBusinessHour(DateTime date, TimeSpan startTime, TimeSpan endTime)
        => date.TimeOfDay >= startTime && date.TimeOfDay < endTime;

    public int GetBusinessDaysBetween(DateTime startDate, DateTime endDate)
    {
        var days = 0;
        for (var date = startDate.Date; date < endDate.Date; date = date.AddDays(1))
        {
            if (!IsWeekend(date))
                days++;
        }

        return days;
    }

    public DateTime ToBusinessLocalTime(DateTime utcDateTime)
        => DateTime.SpecifyKind(
            DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc).AddHours(TimeZoneOffsetHours),
            DateTimeKind.Unspecified);

    public DateTime ToUtc(DateTime businessLocalDateTime)
        => DateTime.SpecifyKind(
            DateTime.SpecifyKind(businessLocalDateTime, DateTimeKind.Unspecified).AddHours(-TimeZoneOffsetHours),
            DateTimeKind.Utc);
}
