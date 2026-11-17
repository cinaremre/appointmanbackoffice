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

        /// <summary>
        /// Converts a UTC instant into the wall-clock time used by the business when its
        /// opening hours are defined. Working hours are stored as local time-of-day values,
        /// so comparing them against a raw UTC timestamp shifts every decision by the
        /// configured offset.
        /// </summary>
        /// <param name="utcDateTime">Instant expressed in UTC.</param>
        /// <returns>The same instant as business local wall-clock time.</returns>
        DateTime ToBusinessLocalTime(DateTime utcDateTime);
    }
}