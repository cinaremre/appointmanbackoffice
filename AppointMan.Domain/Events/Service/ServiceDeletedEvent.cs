using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;

namespace AppointMan.Domain.Events.Service
{
    public class ServiceDeletedEvent : ServiceEventBase
    {
        public string? DeletedBy { get; }

        public ServiceDeletedEvent(
            Guid serviceId,
            Guid businessId,
            string serviceName,
            Money price,
            int durationInMinutes,
            EnumServiceCategory category,
            string? deletedBy = null) : base(serviceId, businessId, serviceName, price, durationInMinutes, category)
        {
            DeletedBy = deletedBy;
        }
    }
}