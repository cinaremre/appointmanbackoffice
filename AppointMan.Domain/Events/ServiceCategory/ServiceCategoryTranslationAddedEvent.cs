namespace AppointMan.Domain.Events.ServiceCategory
{
    public class ServiceCategoryTranslationAddedEvent : DomainEventBase
    {
        public Guid CategoryId { get; }
        public string LanguageCode { get; }
        public string Name { get; }

        public ServiceCategoryTranslationAddedEvent(Guid categoryId, string languageCode, string name) : base()
        {
            CategoryId = categoryId;
            LanguageCode = languageCode;
            Name = name;
        }
    }
}
