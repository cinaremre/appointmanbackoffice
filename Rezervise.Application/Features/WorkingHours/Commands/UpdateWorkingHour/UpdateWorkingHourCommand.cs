using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.WorkingHours.Commands.UpdateWorkingHour
{
    public class UpdateWorkingHourCommand : IRequest<Result<Guid>>
    {
        public Guid WorkingHourId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsClosed { get; set; }
    }
}