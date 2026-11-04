using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Services.Commands.DeactivateService
{
    public class DeactivateServiceCommand : IRequest<Result<Guid>>
    {
        public Guid ServiceId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
        public string? DeactivatedBy { get; set; }
        public string? Reason { get; set; }
    }
}