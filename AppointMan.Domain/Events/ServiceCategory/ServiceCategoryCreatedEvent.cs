namespace AppointMan.Domain.Events.ServiceCategory
{
    public class ServiceCategoryCreatedEvent : DomainEventBase
    {
        public Guid CategoryId { get; }
        public int DisplayOrder { get; }

        public ServiceCategoryCreatedEvent(Guid categoryId, int displayOrder) : base()
        {
            CategoryId = categoryId;
            DisplayOrder = displayOrder;
        }
    }
}
