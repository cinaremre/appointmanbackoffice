using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;

namespace AppointMan.Domain.Events.Service
{
    public class ServiceActivatedEvent : ServiceEventBase
    {
        public string? ActivatedBy { get; }

        public ServiceActivatedEvent(
            Guid serviceId,
            Guid businessId,
            string serviceName,
            Money price,
            int durationInMinutes,
            EnumServiceCategory category,
            string? activatedBy = null) : base(serviceId, businessId, serviceName, price, durationInMinutes, category)
        {
            ActivatedBy = activatedBy;
        }
    }
}