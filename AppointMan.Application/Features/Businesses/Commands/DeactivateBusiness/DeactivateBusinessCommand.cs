using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Businesses.Commands.DeactivateBusiness
{
    public class DeactivateBusinessCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
        public string? DeactivatedBy { get; set; }
        public string? Reason { get; set; }
    }
}