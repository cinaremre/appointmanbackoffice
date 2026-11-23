namespace Rezervise.Domain.Events.Consent
{
    /// <summary>
    /// Bir müşterinin iletişim izni değiştiğinde üretilir (doküman Modül 16).
    /// Aynı değer tekrar atandığında üretilmez.
    /// </summary>
    public class CommunicationConsentChangedEvent : DomainEventBase
    {
        public Guid PreferenceId { get; }
        public Guid BusinessId { get; }
        public Guid CustomerId { get; }
        public Domain.Enums.EnumConsentChannel Channel { get; }
        public Domain.Enums.EnumConsentPurpose Purpose { get; }
        public bool IsAllowed { get; }

        public CommunicationConsentChangedEvent(
            Guid preferenceId,
            Guid businessId,
            Guid customerId,
            Domain.Enums.EnumConsentChannel channel,
            Domain.Enums.EnumConsentPurpose purpose,
            bool isAllowed)
        {
            PreferenceId = preferenceId;
            BusinessId = businessId;
            CustomerId = customerId;
            Channel = channel;
            Purpose = purpose;
            IsAllowed = isAllowed;
        }
    }
}
