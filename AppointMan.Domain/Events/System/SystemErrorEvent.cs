namespace AppointMan.Domain.Events.System
{
    public class SystemErrorEvent : SystemEventBase
    {
        public string ErrorMessage { get; }
        public string? StackTrace { get; }
        public Dictionary<string, object> Context { get; }

        public SystemErrorEvent(
            string errorMessage,
            string? stackTrace = null,
            string? source = null) : base("Error", source)
        {
            ErrorMessage = errorMessage;
            StackTrace = stackTrace;
            Context = new Dictionary<string, object>();
        }

        public void AddContext(string key, object value)
        {
            Context[key] = value;
        }
    }
}