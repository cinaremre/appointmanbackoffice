using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;
using MediatR;

namespace AppointMan.Application.Features.Businesses.Commands.UpdateBusiness
{
    public class UpdateBusinessCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public EnumBusinessType BusinessType { get; set; }
        public Address Address { get; set; }
        public PhoneNumber PhoneNumber { get; set; }
        public Email Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? LogoUrl { get; set; }
    }
}