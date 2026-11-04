using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Commands.AddReview
{
    public class AddReviewCommand : IRequest<Result<Guid>>
    {
        public Guid UserId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? AppointmentId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
}