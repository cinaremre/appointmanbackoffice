using System;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.ValueObjects
{
    public class TimeRange : IEquatable<TimeRange>
    {
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }

        private TimeRange() { } // EF Core icin

        public TimeRange(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime >= endTime)
                throw new DomainException("TimeRange.StartBeforeEnd");

            if (startTime.TotalHours < 0 || endTime.TotalHours > 24)
                throw new DomainException("TimeRange.OutOfDayBounds");

            if (startTime.Minutes % 5 != 0 || endTime.Minutes % 5 != 0)
                throw new DomainException("TimeRange.FiveMinuteStep");

            StartTime = startTime;
            EndTime = endTime;
        }

        public TimeSpan GetDuration()
        {
            return EndTime - StartTime;
        }

        public bool OverlapsWith(TimeRange other)
        {
            if (other is null)
                return false;

            return StartTime < other.EndTime && EndTime > other.StartTime;
        }

        public bool IsWithin(TimeSpan time)
        {
            return time >= StartTime && time <= EndTime;
        }

        public bool Contains(TimeRange other)
        {
            if (other is null)
                return false;

            return StartTime <= other.StartTime && EndTime >= other.EndTime;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as TimeRange);
        }

        public bool Equals(TimeRange? other)
        {
            if (other is null)
                return false;

            return StartTime == other.StartTime && EndTime == other.EndTime;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartTime, EndTime);
        }

        public override string ToString()
        {
            return $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        }
    }
}