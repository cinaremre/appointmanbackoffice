using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.WorkingHours.Commands.SetOpen
{
    public class SetOpenCommand : IRequest<Result<Guid>>
    {
        public Guid WorkingHourId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
    }
}