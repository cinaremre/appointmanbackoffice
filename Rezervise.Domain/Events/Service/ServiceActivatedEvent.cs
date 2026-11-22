using Rezervise.Domain.ValueObjects;

namespace Rezervise.Domain.Events.Service
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
            Guid serviceCategoryId,
            string? activatedBy = null) : base(serviceId, businessId, serviceName, price, durationInMinutes, serviceCategoryId)
        {
            ActivatedBy = activatedBy;
        }
    }
}