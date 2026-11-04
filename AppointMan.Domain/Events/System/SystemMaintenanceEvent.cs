namespace AppointMan.Domain.Events.System
{
    public class SystemMaintenanceEvent : SystemEventBase
    {
        public DateTime StartTime { get; }
        public DateTime? EndTime { get; }
        public string? Reason { get; }

        public SystemMaintenanceEvent(
            DateTime startTime,
            DateTime? endTime = null,
            string? reason = null,
            string? source = null) : base("Maintenance", source)
        {
            StartTime = startTime;
            EndTime = endTime;
            Reason = reason;
        }
    }
}