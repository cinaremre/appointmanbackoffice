using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Businesses.Commands.CreateBusiness
{
    public class CreateBusinessCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public EnumBusinessType BusinessType { get; set; }
        public Address Address { get; set; }
        public PhoneNumber PhoneNumber { get; set; }
        public Email Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? LogoUrl { get; set; }
        public Guid OwnerId { get; set; }
    }
}