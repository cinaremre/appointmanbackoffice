namespace AppointMan.Domain.Events.WorkingHour
{
    public class WorkingHourRemovedEvent : WorkingHourEventBase
    {
        public string? RemovedBy { get; }

        public WorkingHourRemovedEvent(
            Guid workingHourId,
            Guid businessId,
            DayOfWeek dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            string? removedBy = null) : base(workingHourId, businessId, dayOfWeek, startTime, endTime)
        {
            RemovedBy = removedBy;
        }
    }
}