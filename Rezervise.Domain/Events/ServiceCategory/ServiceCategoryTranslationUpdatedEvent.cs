namespace Rezervise.Domain.Events.ServiceCategory
{
    public class ServiceCategoryTranslationUpdatedEvent : DomainEventBase
    {
        public Guid CategoryId { get; }
        public string LanguageCode { get; }
        public string Name { get; }

        public ServiceCategoryTranslationUpdatedEvent(Guid categoryId, string languageCode, string name) : base()
        {
            CategoryId = categoryId;
            LanguageCode = languageCode;
            Name = name;
        }
    }
}
