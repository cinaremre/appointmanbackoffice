namespace AppointMan.Domain.Events.System
{
    public abstract class SystemEventBase : DomainEventBase
    {
        public string EventName { get; }
        public string? Source { get; }

        protected SystemEventBase(
            string eventName,
            string? source = null) : base()
        {
            EventName = eventName;
            Source = source ?? "System";
        }
    }
}