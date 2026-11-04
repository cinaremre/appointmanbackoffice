using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Queries.GetById
{
    public class GetReviewByIdQuery : IRequest<Result<ReviewDto>>
    {
        public Guid ReviewId { get; set; }
    }
}