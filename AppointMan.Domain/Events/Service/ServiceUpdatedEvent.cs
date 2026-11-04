using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;

namespace AppointMan.Domain.Events.Service
{
    public class ServiceUpdatedEvent : ServiceEventBase
    {
        public string? OldName { get; }
        public Money? OldPrice { get; }
        public int? OldDuration { get; }
        public EnumServiceCategory? OldCategory { get; }
        public Dictionary<string, object> Changes { get; }

        public ServiceUpdatedEvent(
            Guid serviceId,
            Guid businessId,
            string serviceName,
            Money price,
            int durationInMinutes,
            EnumServiceCategory category,
            string? oldName = null,
            Money? oldPrice = null,
            int? oldDuration = null,
            EnumServiceCategory? oldCategory = null) : base(serviceId, businessId, serviceName, price, durationInMinutes, category)
        {
            OldName = oldName;
            OldPrice = oldPrice;
            OldDuration = oldDuration;
            OldCategory = oldCategory;
            Changes = new Dictionary<string, object>();
        }

        public void AddChange(string key, object oldValue, object newValue)
        {
            Changes[key] = new { Old = oldValue, New = newValue };
        }
    }
}