using AppointMan.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AppointMan.Infrastructure.Services
{
    public class DateTimeService : IDateTimeService
    {
        /// <summary>Configuration key holding the business time zone offset in hours.</summary>
        private const string TimeZoneOffsetKey = "Business:TimeZoneOffsetHours";

        /// <summary>
        /// Offset used when configuration does not provide one. Turkey has observed a fixed
        /// UTC+3 offset since 2016, so this is a safe default for the current deployment.
        /// </summary>
        private const double DefaultTimeZoneOffsetHours = 3d;

        private readonly TimeSpan _businessTimeZoneOffset;

        public DateTimeService()
            : this(null)
        {
        }

        /// <summary>
        /// Initialises the service with the configured business time zone offset.
        /// </summary>
        /// <param name="configuration">
        /// Application configuration; when null or missing the key, the built-in default is used.
        /// </param>
        public DateTimeService(IConfiguration? configuration)
        {
            var configuredOffset = configuration?[TimeZoneOffsetKey];

            // An unparsable value must not break startup: fall back to the documented default.
            var offsetHours = double.TryParse(configuredOffset, out var parsed)
                ? parsed
                : DefaultTimeZoneOffsetHours;

            _businessTimeZoneOffset = TimeSpan.FromHours(offsetHours);
        }

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

        /// <inheritdoc />
        public DateTime ToBusinessLocalTime(DateTime utcDateTime)
        {
            // Treat an unspecified kind as UTC: the API contract states timestamps are UTC.
            // Converting a Local kind again would apply the offset twice.
            if (utcDateTime.Kind == DateTimeKind.Local)
                utcDateTime = utcDateTime.ToUniversalTime();

            return DateTime.SpecifyKind(utcDateTime.Add(_businessTimeZoneOffset), DateTimeKind.Unspecified);
        }
    }
}
