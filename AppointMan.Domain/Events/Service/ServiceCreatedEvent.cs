using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;

namespace AppointMan.Domain.Events.Service
{
    public class ServiceCreatedEvent : ServiceEventBase
    {
        public ServiceCreatedEvent(
            Guid serviceId,
            Guid businessId,
            string serviceName,
            Money price,
            int durationInMinutes,
            EnumServiceCategory category) : base(serviceId, businessId, serviceName, price, durationInMinutes, category)
        {
        }
    }
}