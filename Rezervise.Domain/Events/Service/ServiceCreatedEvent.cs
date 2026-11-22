using Rezervise.Domain.ValueObjects;

namespace Rezervise.Domain.Events.Service
{
    public class ServiceCreatedEvent : ServiceEventBase
    {
        public ServiceCreatedEvent(
            Guid serviceId,
            Guid businessId,
            string serviceName,
            Money price,
            int durationInMinutes,
            Guid serviceCategoryId) : base(serviceId, businessId, serviceName, price, durationInMinutes, serviceCategoryId)
        {
        }
    }
}