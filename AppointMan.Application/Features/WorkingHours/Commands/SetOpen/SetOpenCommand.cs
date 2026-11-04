using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.WorkingHours.Commands.SetOpen
{
    public class SetOpenCommand : IRequest<Result<Guid>>
    {
        public Guid WorkingHourId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
    }
}