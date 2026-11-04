using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Commands.ShowReview
{
    public class ShowReviewCommand : IRequest<Result<Guid>>
    {
        public Guid ReviewId { get; set; }
    }
}