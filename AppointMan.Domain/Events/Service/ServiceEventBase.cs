using AppointMan.Domain.Enums;
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
        public EnumServiceCategory Category { get; }

        protected ServiceEventBase(
            Guid serviceId,
            Guid businessId,
            string serviceName,
            Money price,
            int durationInMinutes,
            EnumServiceCategory category) : base()
        {
            ServiceId = serviceId;
            BusinessId = businessId;
            ServiceName = serviceName;
            Price = price;
            DurationInMinutes = durationInMinutes;
            Category = category;
        }
    }
}