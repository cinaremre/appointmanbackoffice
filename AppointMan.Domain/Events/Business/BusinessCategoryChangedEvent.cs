using AppointMan.Domain.Enums;

namespace AppointMan.Domain.Events.Business
{
    public class BusinessCategoryChangedEvent : BusinessEventBase
    {
        public EnumBusinessType OldCategory { get; }
        public EnumBusinessType NewCategory { get; }
        public string? ChangedBy { get; }

        public BusinessCategoryChangedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType oldCategory,
            EnumBusinessType newCategory,
            string? changedBy = null) : base(businessId, businessName, ownerId, newCategory)
        {
            OldCategory = oldCategory;
            NewCategory = newCategory;
            ChangedBy = changedBy;
        }
    }
}