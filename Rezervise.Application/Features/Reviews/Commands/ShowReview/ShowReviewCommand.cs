using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Reviews.Commands.ShowReview
{
    public class ShowReviewCommand : IRequest<Result<Guid>>
    {
        public Guid ReviewId { get; set; }
    }
}