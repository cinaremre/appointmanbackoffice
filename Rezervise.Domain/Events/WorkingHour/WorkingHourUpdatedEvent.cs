namespace Rezervise.Domain.Events.WorkingHour
{
    public class WorkingHourUpdatedEvent : WorkingHourEventBase
    {
        public TimeSpan OldStartTime { get; }
        public TimeSpan OldEndTime { get; }

        public WorkingHourUpdatedEvent(
            Guid workingHourId,
            Guid businessId,
            DayOfWeek dayOfWeek,
            TimeSpan oldStartTime,
            TimeSpan oldEndTime,
            TimeSpan newStartTime,
            TimeSpan newEndTime) : base(workingHourId, businessId, dayOfWeek, newStartTime, newEndTime)
        {
            OldStartTime = oldStartTime;
            OldEndTime = oldEndTime;
        }
    }
}