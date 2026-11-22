namespace Rezervise.Domain.Events.WorkingHour
{
    public class WorkingHourAddedEvent : WorkingHourEventBase
    {
        public WorkingHourAddedEvent(
            Guid workingHourId,
            Guid businessId,
            DayOfWeek dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime) : base(workingHourId, businessId, dayOfWeek, startTime, endTime)
        {
        }
    }
}