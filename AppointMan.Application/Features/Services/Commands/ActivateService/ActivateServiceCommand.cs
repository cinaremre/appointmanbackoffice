using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Services.Commands.ActivateService
{
    public class ActivateServiceCommand : IRequest<Result<Guid>>
    {
        public Guid ServiceId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
        public string? ActivatedBy { get; set; }
    }
}