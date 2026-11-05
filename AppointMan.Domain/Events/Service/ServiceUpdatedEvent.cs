using AppointMan.Domain.ValueObjects;

namespace AppointMan.Domain.Events.Service
{
    public class ServiceUpdatedEvent : ServiceEventBase
    {
        public string? OldName { get; }
        public Money? OldPrice { get; }
        public int? OldDuration { get; }
        public Guid? OldServiceCategoryId { get; }
        public Dictionary<string, object> Changes { get; }

        public ServiceUpdatedEvent(
            Guid serviceId,
            Guid businessId,
            string serviceName,
            Money price,
            int durationInMinutes,
            Guid serviceCategoryId,
            string? oldName = null,
            Money? oldPrice = null,
            int? oldDuration = null,
            Guid? oldServiceCategoryId = null) : base(serviceId, businessId, serviceName, price, durationInMinutes, serviceCategoryId)
        {
            OldName = oldName;
            OldPrice = oldPrice;
            OldDuration = oldDuration;
            OldServiceCategoryId = oldServiceCategoryId;
            Changes = new Dictionary<string, object>();
        }

        public void AddChange(string key, object oldValue, object newValue)
        {
            Changes[key] = new { Old = oldValue, New = newValue };
        }
    }
}