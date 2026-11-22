using Rezervise.Domain.ValueObjects;

namespace Rezervise.Domain.Events.Service
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
            Guid serviceCategoryId,
            string? deletedBy = null) : base(serviceId, businessId, serviceName, price, durationInMinutes, serviceCategoryId)
        {
            DeletedBy = deletedBy;
        }
    }
}