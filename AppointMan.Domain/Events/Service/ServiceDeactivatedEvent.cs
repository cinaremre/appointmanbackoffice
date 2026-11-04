using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;

namespace AppointMan.Domain.Events.Service
{
    public class ServiceDeactivatedEvent : ServiceEventBase
    {
        public string? DeactivatedBy { get; }
        public string? Reason { get; }

        public ServiceDeactivatedEvent(
            Guid serviceId,
            Guid businessId,
            string serviceName,
            Money price,
            int durationInMinutes,
            EnumServiceCategory category,
            string? deactivatedBy = null,
            string? reason = null) : base(serviceId, businessId, serviceName, price, durationInMinutes, category)
        {
            DeactivatedBy = deactivatedBy;
            Reason = reason;
        }
    }
}