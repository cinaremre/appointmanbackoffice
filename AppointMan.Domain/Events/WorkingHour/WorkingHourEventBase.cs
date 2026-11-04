namespace AppointMan.Domain.Events.WorkingHour
{
    public abstract class WorkingHourEventBase : DomainEventBase
    {
        public Guid WorkingHourId { get; }
        public Guid BusinessId { get; }
        public DayOfWeek DayOfWeek { get; }
        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }

        protected WorkingHourEventBase(
            Guid workingHourId,
            Guid businessId,
            DayOfWeek dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime) : base()
        {
            WorkingHourId = workingHourId;
            BusinessId = businessId;
            DayOfWeek = dayOfWeek;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}