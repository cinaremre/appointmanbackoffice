using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.WorkingHours.Commands.RemoveWorkingHour
{
    public class RemoveWorkingHourCommand : IRequest<Result<Guid>>
    {
        public Guid WorkingHourId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
    }
}