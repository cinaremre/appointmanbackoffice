using Rezervise.Domain.Enums;
using Rezervise.Domain.ValueObjects;

namespace Rezervise.Domain.Events.Business
{
    public class BusinessCreatedEvent : BusinessEventBase
    {
        public string OwnerEmail { get; }
        public Address Address { get; }
        public PhoneNumber PhoneNumber { get; }
        public Email Email { get; }

        public BusinessCreatedEvent(
            Guid businessId,
            string businessName,
            Guid ownerId,
            EnumBusinessType businessType,
            string ownerEmail,
            Address address,
            PhoneNumber phoneNumber,
            Email email) : base(businessId, businessName, ownerId, businessType)
        {
            OwnerEmail = ownerEmail;
            Address = address;
            PhoneNumber = phoneNumber;
            Email = email;
        }
    }
}