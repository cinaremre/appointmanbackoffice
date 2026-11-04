using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Commands.DeleteReview
{
    public class DeleteReviewCommand : IRequest<Result<Guid>>
    {
        public Guid ReviewId { get; set; }
        public Guid UserId { get; set; }
    }
}