using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Businesses.Commands.ActivateBusiness
{
    public class ActivateBusinessCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
        public string? ActivatedBy { get; set; }
    }
}