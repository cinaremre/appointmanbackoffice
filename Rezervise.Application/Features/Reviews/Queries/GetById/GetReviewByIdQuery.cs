using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Reviews.Queries.GetById
{
    public class GetReviewByIdQuery : IRequest<Result<ReviewDto>>
    {
        public Guid ReviewId { get; set; }
    }
}