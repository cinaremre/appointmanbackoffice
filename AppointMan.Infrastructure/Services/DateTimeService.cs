using AppointMan.Domain.Interfaces;

namespace AppointMan.Infrastructure.Services
{
    public class DateTimeService : IDateTimeService
    {
        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Today => DateTime.Today;
        public DateTimeOffset OffsetNow => DateTimeOffset.Now;
        public DateTimeOffset OffsetUtcNow => DateTimeOffset.UtcNow;

        public bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        public bool IsBusinessHour(DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            var timeOfDay = date.TimeOfDay;
            return timeOfDay >= startTime && timeOfDay <= endTime;
        }

        public int GetBusinessDaysBetween(DateTime startDate, DateTime endDate)
        {
            var days = 0;
            var current = startDate.Date;
            var end = endDate.Date;

            while (current <= end)
            {
                if (!IsWeekend(current))
                    days++;
                current = current.AddDays(1);
            }

            return days;
        }
    }
}