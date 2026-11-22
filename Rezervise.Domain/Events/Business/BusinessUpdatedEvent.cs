using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.Business
{
    public class BusinessUpdatedEvent : BusinessEventBase
    {
        public string? UpdatedBy { get; }
        public Dictionary<string, object> Changes { get; }

        public BusinessUpdatedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType,
            string? updatedBy = null) : base(businessId, businessName, ownerId, businessType)
        {
            UpdatedBy = updatedBy;
            Changes = new Dictionary<string, object>();
        }

        public void AddChange(string key, object oldValue, object newValue)
        {
            Changes[key] = new { Old = oldValue, New = newValue };
        }
    }
}