using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.WorkingHours.Commands.SetClosed
{
    public class SetClosedCommand : IRequest<Result<Guid>>
    {
        public Guid WorkingHourId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
    }
}