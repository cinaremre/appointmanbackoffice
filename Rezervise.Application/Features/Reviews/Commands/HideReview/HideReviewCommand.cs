using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Reviews.Commands.HideReview
{
    public class HideReviewCommand : IRequest<Result<Guid>>
    {
        public Guid ReviewId { get; set; }
        public string? HiddenBy { get; set; }
        public string? Reason { get; set; }
    }
}