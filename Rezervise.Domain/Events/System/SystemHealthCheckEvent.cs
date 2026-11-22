namespace Rezervise.Domain.Events.System
{
    public class SystemHealthCheckEvent : SystemEventBase
    {
        public bool IsHealthy { get; }
        public string? Status { get; }

        public SystemHealthCheckEvent(
            bool isHealthy,
            string? status = null,
            string? source = null) : base("HealthCheck", source)
        {
            IsHealthy = isHealthy;
            Status = status;
        }
    }
}