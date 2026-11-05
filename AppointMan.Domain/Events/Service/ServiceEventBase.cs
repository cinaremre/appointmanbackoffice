using AppointMan.Domain.ValueObjects;

namespace AppointMan.Domain.Events.Service
{
    public abstract class ServiceEventBase : DomainEventBase
    {
        public Guid ServiceId { get; }
        public Guid BusinessId { get; }
        public string ServiceName { get; }
        public Money Price { get; }
        public int DurationInMinutes { get; }
        public Guid ServiceCategoryId { get; }

        protected ServiceEventBase(
            Guid serviceId,
            Guid businessId,
            string serviceName,
            Money price,
            int durationInMinutes,
            Guid serviceCategoryId) : base()
        {
            ServiceId = serviceId;
            BusinessId = businessId;
            ServiceName = serviceName;
            Price = price;
            DurationInMinutes = durationInMinutes;
            ServiceCategoryId = serviceCategoryId;
        }
    }
}